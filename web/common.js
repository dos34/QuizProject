// Общие функции фронта: HTTP-клиент, SignalR, таймер.
const Api = {
  async request(method, url, body, token) {
    const headers = { 'Content-Type': 'application/json' };
    if (token) headers.Authorization = `Bearer ${token}`;
    const res = await fetch(url, { method, headers, body: body ? JSON.stringify(body) : undefined });
    const text = await res.text();
    const data = text ? JSON.parse(text) : null;
    if (!res.ok) {
      const msg = data?.detail || data?.title || `HTTP ${res.status}`;
      const errors = data?.errors ? Object.values(data.errors).flat().join(' ') : '';
      throw new Error(errors ? `${msg} ${errors}` : msg);
    }
    return data;
  },
  get: (url, token) => Api.request('GET', url, null, token),
  post: (url, body, token) => Api.request('POST', url, body, token),
  put: (url, body, token) => Api.request('PUT', url, body, token),
  del: (url, token) => Api.request('DELETE', url, null, token),
};

const $ = (sel) => document.querySelector(sel);
const esc = (s) => String(s ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
function show(id) { document.querySelectorAll('[data-screen]').forEach((el) => el.classList.toggle('hidden', el.id !== id)); }

// Подключение к хабу. onState вызывается после (пере)подключения — клиент забирает актуальное состояние по REST.
async function connectHub(gameId, token, handlers, onState) {
  const conn = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/game', { accessTokenFactory: () => token })
    .withAutomaticReconnect()
    .build();
  for (const [name, fn] of Object.entries(handlers)) conn.on(name, fn);
  conn.onreconnected(async () => { await conn.invoke('JoinGame', gameId); onState && onState(); });
  await conn.start();
  await conn.invoke('JoinGame', gameId);
  return conn;
}

// Таймер по серверному времени: offset = serverTime - локальное время.
let clockOffset = 0;
function syncClock(serverTime) { clockOffset = new Date(serverTime).getTime() - Date.now(); }
let timerHandle = null;
function runTimer(el, deadline) {
  clearInterval(timerHandle);
  const end = new Date(deadline).getTime();
  const tick = () => {
    const left = Math.max(0, Math.ceil((end - (Date.now() + clockOffset)) / 1000));
    el.textContent = left;
    if (left <= 0) clearInterval(timerHandle);
  };
  tick();
  timerHandle = setInterval(tick, 250);
}
function stopTimer() { clearInterval(timerHandle); }

function leaderboardTable(entries, meId) {
  if (!entries?.length) return '<p class="muted">Пока никого</p>';
  return `<table><tr><th>#</th><th>Игрок</th><th>Очки</th></tr>${entries
    .map((e) => `<tr class="${e.playerId === meId ? 'me' : ''}"><td>${e.rank}</td><td>${esc(e.nickname)}</td><td>${e.score}</td></tr>`)
    .join('')}</table>`;
}
