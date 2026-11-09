using MongoDB.Driver;
using Quiz.Api.Dtos;
using Quiz.Api.Errors;
using Quiz.Domain.Entities;
using Quiz.Domain.Rules;
using Quiz.Infrastructure.Mongo;

namespace Quiz.Api.Services;

public sealed class QuizService(MongoContext db, TimeProvider time)
{
    public async Task<IReadOnlyList<QuizSummary>> ListAsync(Guid ownerId, CancellationToken ct)
    {
        var quizzes = await db.Quizzes
            .Find(q => q.OwnerId == ownerId)
            .SortByDescending(q => q.UpdatedAt)
            .ToListAsync(ct);

        return quizzes.Select(q => new QuizSummary(q.Id, q.Title, q.Questions.Count, q.UpdatedAt)).ToList();
    }

    public async Task<QuizResponse> GetAsync(Guid ownerId, Guid id, CancellationToken ct) =>
        QuizResponse.From(await LoadOwnedAsync(ownerId, id, ct));

    public async Task<QuizResponse> CreateAsync(Guid ownerId, QuizRequest request, CancellationToken ct)
    {
        var questions = ToEntities(request);
        Validate(request.Title, questions);

        var now = time.GetUtcNow().UtcDateTime;
        var quiz = new QuizEntity
        {
            OwnerId = ownerId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Questions = questions,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await db.Quizzes.InsertOneAsync(quiz, cancellationToken: ct);
        return QuizResponse.From(quiz);
    }

    public async Task<QuizResponse> UpdateAsync(Guid ownerId, Guid id, QuizRequest request, CancellationToken ct)
    {
        var questions = ToEntities(request);
        Validate(request.Title, questions);

        var update = Builders<QuizEntity>.Update
            .Set(q => q.Title, request.Title.Trim())
            .Set(q => q.Description, request.Description?.Trim())
            .Set(q => q.Questions, questions)
            .Set(q => q.UpdatedAt, time.GetUtcNow().UtcDateTime);

        var updated = await db.Quizzes.FindOneAndUpdateAsync<QuizEntity>(
            q => q.Id == id && q.OwnerId == ownerId,
            update,
            new FindOneAndUpdateOptions<QuizEntity> { ReturnDocument = ReturnDocument.After },
            ct);

        return QuizResponse.From(updated ?? throw new NotFoundException("Quiz not found."));
    }

    public async Task DeleteAsync(Guid ownerId, Guid id, CancellationToken ct)
    {
        var result = await db.Quizzes.DeleteOneAsync(q => q.Id == id && q.OwnerId == ownerId, ct);
        if (result.DeletedCount == 0)
        {
            throw new NotFoundException("Quiz not found.");
        }
    }

    public async Task<QuizEntity> LoadOwnedAsync(Guid ownerId, Guid id, CancellationToken ct)
    {
        // Чужую викторину отдаём как 404, а не 403 — не раскрываем существование.
        var quiz = await db.Quizzes.Find(q => q.Id == id && q.OwnerId == ownerId).FirstOrDefaultAsync(ct);
        return quiz ?? throw new NotFoundException("Quiz not found.");
    }

    private static List<Question> ToEntities(QuizRequest request) =>
        (request.Questions ?? []).Select(q => q.ToEntity()).ToList();

    private static void Validate(string title, List<Question> questions)
    {
        var errors = QuizValidator.Validate(title, questions);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}
