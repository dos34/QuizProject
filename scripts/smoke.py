#!/usr/bin/env python3
"""
Smoke-тест основного сценария через REST (только stdlib):
регистрация ведущего → викторина → игра + PIN → 2 игрока → старт →
ответы → закрытие вопроса (все ответили / таймер) → «далее» → итоги.

Запуск: python3 scripts/smoke.py [http://localhost:8080]
"""
import json
import sys
import time
import urllib.error
import urllib.request
import uuid

BASE = (sys.argv[1] if len(sys.argv) > 1 else "http://localhost:8080").rstrip("/")


def call(method, path, body=None, token=None, expect=None):
    req = urllib.request.Request(BASE + path, method=method)
    req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", f"Bearer {token}")
    data = json.dumps(body).encode() if body is not None else None
    try:
        with urllib.request.urlopen(req, data, timeout=10) as r:
            status, text = r.status, r.read().decode()
    except urllib.error.HTTPError as e:
        status, text = e.code, e.read().decode()
    if expect and status != expect:
        raise AssertionError(f"{method} {path}: expected {expect}, got {status}: {text}")
    return status, (json.loads(text) if text else None)


def wait_state(game_id, token, predicate, timeout=40):
    deadline = time.time() + timeout
    while time.time() < deadline:
        _, s = call("GET", f"/api/games/{game_id}", token=token, expect=200)
        if predicate(s):
            return s
        time.sleep(0.3)
    raise AssertionError(f"state not reached, last: {s}")


def step(msg):
    print(f"  ✓ {msg}")


login = f"host_{uuid.uuid4().hex[:8]}"
_, host = call("POST", "/api/auth/register", {"login": login, "password": "secret123"}, expect=201)
token = host["token"]
step(f"ведущий {login} зарегистрирован")

_, quiz = call("POST", "/api/quizzes", {
    "title": "Smoke quiz",
    "questions": [
        {"text": "2+2?", "options": ["3", "4"], "correctOptionIndex": 1, "timeLimitSeconds": 10},
        {"text": "Небо?", "options": ["синее", "зелёное", "красное"], "correctOptionIndex": 0, "timeLimitSeconds": 5},
    ],
}, token=token, expect=201)
step("викторина создана")

status, _ = call("POST", "/api/quizzes", {"title": "", "questions": []}, token=token)
assert status == 400, status
step("невалидная викторина → 400")

_, game = call("POST", "/api/games", {"quizId": quiz["id"]}, token=token, expect=201)
gid, pin = game["gameId"], game["pin"]
step(f"игра создана, PIN {pin}")

_, alice = call("POST", "/api/games/join", {"pin": pin, "nickname": "Alice"}, expect=200)
_, bob = call("POST", "/api/games/join", {"pin": pin, "nickname": "Bob"}, expect=200)
status, _ = call("POST", "/api/games/join", {"pin": pin, "nickname": "alice"})
assert status == 409, status
step("2 игрока вошли, дубль никнейма → 409")

call("POST", f"/api/games/{gid}/start", token=token, expect=202)
status, _ = call("POST", f"/api/games/{gid}/start", token=token)
assert status == 409, status
step("игра запущена, повторный старт → 409")

s = wait_state(gid, alice["token"], lambda s: s["phase"] == "Open" and s["currentQuestionIndex"] == 0)
assert s["currentQuestion"]["correctOptionIndex"] is None, "правильный ответ утёк клиенту!"
step("Worker открыл вопрос 1, правильный ответ скрыт")

call("POST", f"/api/games/{gid}/answers", {"questionIndex": 0, "optionIndex": 1}, token=alice["token"], expect=202)
status, _ = call("POST", f"/api/games/{gid}/answers", {"questionIndex": 0, "optionIndex": 0}, token=alice["token"])
assert status == 409, status
time.sleep(1)
call("POST", f"/api/games/{gid}/answers", {"questionIndex": 0, "optionIndex": 0}, token=bob["token"], expect=202)
step("ответы приняты, повторный ответ → 409")

s = wait_state(gid, token, lambda s: s["phase"] == "Closed" and s["currentQuestionIndex"] == 0, timeout=5)
assert s["currentQuestion"]["correctOptionIndex"] == 1
step("все ответили → вопрос закрыт досрочно")

_, lb = call("GET", f"/api/games/{gid}/leaderboard", token=token, expect=200)
assert lb[0]["nickname"] == "Alice" and lb[0]["score"] > 900 and lb[1]["score"] == 0, lb
step(f"очки посчитаны: {[(e['nickname'], e['score']) for e in lb]}")

call("POST", f"/api/games/{gid}/next", token=token, expect=202)
call("POST", f"/api/games/{gid}/next", token=token)  # двойной клик «Далее» — не должен перескочить вопрос
wait_state(gid, token, lambda s: s["phase"] == "Open" and s["currentQuestionIndex"] == 1)
time.sleep(1.5)
s = wait_state(gid, token, lambda s: True)
assert s["currentQuestionIndex"] == 1, s
step("«Далее» → вопрос 2 (двойной клик не перескочил вопрос)")

call("POST", f"/api/games/{gid}/answers", {"questionIndex": 1, "optionIndex": 0}, token=bob["token"], expect=202)
t0 = time.time()
wait_state(gid, token, lambda s: s["phase"] == "Closed" and s["currentQuestionIndex"] == 1, timeout=15)
step(f"вопрос 2 закрыт по таймеру (отложенное сообщение) через {time.time() - t0:.1f}с")

status, _ = call("POST", f"/api/games/{gid}/answers", {"questionIndex": 1, "optionIndex": 0}, token=alice["token"])
assert status == 409, status
step("ответ после закрытия → 409")

call("POST", f"/api/games/{gid}/next", token=token, expect=202)
wait_state(gid, token, lambda s: s["status"] == "Finished")
_, lb = call("GET", f"/api/games/{gid}/leaderboard", token=token, expect=200)
step(f"игра завершена, итоги: {[(e['rank'], e['nickname'], e['score']) for e in lb]}")

status, _ = call("GET", f"/api/games/{gid}", token=alice["token"])
assert status == 200
status, _ = call("GET", f"/api/quizzes", token=alice["token"])
assert status == 403, status
step("токен игрока не даёт доступа к API ведущего → 403")

print("\nSMOKE OK")
