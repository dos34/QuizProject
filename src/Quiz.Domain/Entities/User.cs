namespace Quiz.Domain.Entities;

/// <summary>Ведущий (автор викторин). Игроки анонимные и пользователями не являются.</summary>
public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Login { get; set; }
    public required string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
