using Jex.Application.Common.Interfaces;
using Jex.Domain.Entities;

namespace Jex.IntegrationTests.Persistence;

/// <summary>Repository behaviour against a real SQLite database: change tracking, audit stamps, paging.</summary>
public sealed class RepositoryTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task UpdateAsync_TrackedEntity_WritesOnlyChangedColumns()
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var freeSql = scope.ServiceProvider.GetRequiredService<IFreeSql>();

        var created = await repository.AddAsync(new User
        {
            UserName = "tracked@example.com",
            Email = "tracked@example.com",
            NormalizedUserName = "TRACKED@EXAMPLE.COM",
            NormalizedEmail = "TRACKED@EXAMPLE.COM",
            FirstName = "Before",
            LastName = "Tracking",
        });

        var loaded = await repository.GetByIdAsync(created.Id);
        loaded.ShouldNotBeNull();
        loaded.FirstName = "After";

        var updateSql = new List<string>();
        void Capture(object? sender, FreeSql.Aop.CurdAfterEventArgs e)
        {
            if (e.CurdType == FreeSql.Aop.CurdType.Update)
            {
                updateSql.Add(e.Sql);
            }
        }

        freeSql.Aop.CurdAfter += Capture;
        try
        {
            await repository.UpdateAsync(loaded);
        }
        finally
        {
            freeSql.Aop.CurdAfter -= Capture;
        }

        var sql = updateSql.ShouldHaveSingleItem();
        sql.ShouldContain("FirstName");
        sql.ShouldContain("LastModifiedAt");
        sql.ShouldNotContain("PasswordHash", customMessage: "untouched columns must not be rewritten");
        sql.ShouldNotContain("CreatedAt", customMessage: "creation audit fields are never updated");
    }

    [Fact]
    public async Task AddAsync_StampsCreatedAt()
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var created = await repository.AddAsync(new User
        {
            UserName = "audit@example.com",
            Email = "audit@example.com",
            NormalizedUserName = "AUDIT@EXAMPLE.COM",
            NormalizedEmail = "AUDIT@EXAMPLE.COM",
            FirstName = "Audit",
            LastName = "Stamp",
        });

        created.CreatedAt.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
        created.LastModifiedAt.ShouldBeNull();
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsTotalCountAndPage()
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        for (var i = 0; i < 3; i++)
        {
            await repository.AddAsync(new User
            {
                UserName = $"page{i}@example.com",
                Email = $"page{i}@example.com",
                NormalizedUserName = $"PAGE{i}@EXAMPLE.COM",
                NormalizedEmail = $"PAGE{i}@EXAMPLE.COM",
                FirstName = "Page",
                LastName = i.ToString(System.Globalization.CultureInfo.InvariantCulture),
            });
        }

        var page = await repository.GetPagedAsync(1, 2, u => u.FirstName == "Page");

        page.TotalCount.ShouldBe(3);
        page.Items.Count.ShouldBe(2);
        page.Page.ShouldBe(1);
        page.PageSize.ShouldBe(2);
    }
}
