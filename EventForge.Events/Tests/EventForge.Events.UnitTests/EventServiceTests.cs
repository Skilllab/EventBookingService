using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using EventForge.CacheKeys;
using EventForge.Events.Application.DTO;
using EventForge.Events.Application.Entities;
using EventForge.Events.Application.Interfaces;
using EventForge.Events.Application.Services;
using EventForge.Events.Domain.Entities;
using EventForge.Events.Domain.Exceptions;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Moq;

using Xunit;

namespace EventForge.Events.UnitTests;

public class EventServiceTests
{
    private static readonly FakeTimeProvider FakeTime = new(new DateTimeOffset(2025, 7, 4, 10, 0, 0, TimeSpan.Zero));
    private static readonly IOptions<RedisOptions> RedisOptions = Options.Create(new RedisOptions { SingleEventExpirationMinutes = 5, TopEventsExpirationMinutes = 3 });


    [Fact]
    [Trait("Category", "CreateEvent")]
    public async Task CreateEventAsync_Should_Return_EventDto_And_Save_Event()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var service = new EventService(repositoryMock.Object, Mock.Of<ILogger<EventService>>(), Mock.Of<ICacheService>(), RedisOptions, FakeTime);
        var startAt = FakeTime.GetUtcNow().UtcDateTime.AddDays(1);
        var dto = new CreateEventDto("Намечается баня", startAt, startAt.AddHours(2), 25, "Раз в неделю точно");

        Event? savedEvent = null;
        repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback<Event, CancellationToken>((evt, _) => savedEvent = evt)
            .Returns(Task.CompletedTask);

        var result = await service.CreateEventAsync(dto, CancellationToken.None);

        result.Title.Should().Be("Намечается баня");
        result.TotalSeats.Should().Be(25);
        result.AvailableSeats.Should().Be(25);
        savedEvent.Should().NotBeNull();
        savedEvent!.Title.Should().Be("Намечается баня");
        repositoryMock.Verify(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "GetEvent")]
    public async Task GetEventAsync_CacheHit_Should_Return_From_Cache_And_Not_Call_Repository()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var cacheMock = new Mock<ICacheService>();
        var service = new EventService(repositoryMock.Object, Mock.Of<ILogger<EventService>>(), cacheMock.Object, RedisOptions, FakeTime);
        var eventId = Guid.NewGuid();
        var cachedDto = new EventDTO(eventId, "Из кэша", "Описание", FakeTime.GetUtcNow().UtcDateTime.AddDays(1), FakeTime.GetUtcNow().UtcDateTime.AddDays(1).AddHours(2), 50, 45);
        var cacheKey = KeysForEvents.ForEvent(eventId);

        cacheMock
            .Setup(x => x.GetStringAsync(cacheKey))
            .ReturnsAsync(JsonSerializer.Serialize(cachedDto));

        var result = await service.GetEventAsync(eventId, CancellationToken.None);

        result.Id.Should().Be(eventId);
        result.Title.Should().Be("Из кэша");
        repositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        cacheMock.Verify(x => x.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "GetEvent")]
    public async Task GetEventAsync_CacheMiss_Should_Call_Repository_And_Set_Cache()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var cacheMock = new Mock<ICacheService>();
        var service = new EventService(repositoryMock.Object, Mock.Of<ILogger<EventService>>(), cacheMock.Object, RedisOptions, FakeTime);
        var startAt = FakeTime.GetUtcNow().UtcDateTime.AddDays(1);
        var evt = Event.Create("Конференция", startAt, startAt.AddHours(8), 100, "Описание");
        var cacheKey = KeysForEvents.ForEvent(evt.Id);

        cacheMock.Setup(x => x.GetStringAsync(cacheKey)).ReturnsAsync((string?) null);
        repositoryMock.Setup(x => x.GetByIdAsync(evt.Id, It.IsAny<CancellationToken>())).ReturnsAsync(evt);

        var result = await service.GetEventAsync(evt.Id, CancellationToken.None);

        result.Title.Should().Be("Конференция");
        repositoryMock.Verify(x => x.GetByIdAsync(evt.Id, It.IsAny<CancellationToken>()), Times.Once);
        cacheMock.Verify(x => x.SetStringAsync(cacheKey, It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "GetEvent")]
    public async Task GetEventAsync_Should_Throw_NotFound_When_Not_In_Repository()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var cacheMock = new Mock<ICacheService>();
        var service = new EventService(repositoryMock.Object, Mock.Of<ILogger<EventService>>(), cacheMock.Object, RedisOptions, FakeTime);
        var eventId = Guid.NewGuid();

        cacheMock.Setup(x => x.GetStringAsync(KeysForEvents.ForEvent(eventId))).ReturnsAsync((string?) null);
        repositoryMock.Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync((Event?) null);

        Func<Task> act = () => service.GetEventAsync(eventId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        cacheMock.Verify(x => x.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "GetTop10Events")]
    public async Task GetTop10EventsAsync_CacheHit_Should_Return_From_Cache_And_Not_Call_Repository()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var cacheMock = new Mock<ICacheService>();
        var service = new EventService(repositoryMock.Object, Mock.Of<ILogger<EventService>>(), cacheMock.Object, RedisOptions, FakeTime);
        var cachedResult = new PaginatedResultTop10DTO(new List<EventDTO>());
        var cacheKey = KeysForEvents.TopEvents;

        cacheMock
            .Setup(x => x.GetStringAsync(cacheKey))
            .ReturnsAsync(JsonSerializer.Serialize(cachedResult));

        var result = await service.GetTop10EventsAsync(CancellationToken.None);

        result.Events.Should().BeEmpty();
        repositoryMock.Verify(x => x.GetTop10EventsAsync(It.IsAny<CancellationToken>()), Times.Never);
        cacheMock.Verify(x => x.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "GetTop10Events")]
    public async Task GetTop10EventsAsync_CacheMiss_Should_Call_Repository_And_Set_Cache()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var cacheMock = new Mock<ICacheService>();
        var service = new EventService(repositoryMock.Object, Mock.Of<ILogger<EventService>>(), cacheMock.Object, RedisOptions, FakeTime);
        var cacheKey = KeysForEvents.TopEvents;
        var startAt = FakeTime.GetUtcNow().UtcDateTime.AddDays(1);
        var evt = Event.Create("Топ событие", startAt, startAt.AddHours(2), 100, "Описание");
        var pagedResult = new Top10PagedResult<Event>(new List<Event> { evt });

        cacheMock.Setup(x => x.GetStringAsync(cacheKey)).ReturnsAsync((string?) null);
        repositoryMock.Setup(x => x.GetTop10EventsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(pagedResult);

        var result = await service.GetTop10EventsAsync(CancellationToken.None);

        result.Events.Should().HaveCount(1);
        result.Events[0].Title.Should().Be("Топ событие");
        repositoryMock.Verify(x => x.GetTop10EventsAsync(It.IsAny<CancellationToken>()), Times.Once);
        cacheMock.Verify(x => x.SetStringAsync(cacheKey, It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "GetEvents")]
    public async Task GetEventsAsync_Should_Return_Paginated_Result_From_Repository()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var cacheMock = new Mock<ICacheService>();
        var service = new EventService(repositoryMock.Object, Mock.Of<ILogger<EventService>>(), cacheMock.Object, RedisOptions, FakeTime);
        var filter = new EventsFilterDTO(title: "компьютерным", page: 2, pageSize: 1);
        var startAt = FakeTime.GetUtcNow().UtcDateTime.AddDays(1);
        var eventItem = Event.Create("Конференция по новым компьютерным технологиям", startAt, startAt.AddHours(2), 50, "Описание");
        var paged = new PagedResult<Event>(new List<Event> { eventItem }, 3);

        repositoryMock
            .Setup(x => x.GetPagedAsync(filter.Title, filter.From, filter.To, filter.Page, filter.PageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var result = await service.GetEventsAsync(filter, CancellationToken.None);

        result.EventsTotalCount.Should().Be(3);
        result.CurrentPageNumber.Should().Be(2);
        result.EventsCountOnCurrentPage.Should().Be(1);
        result.Events.Should().ContainSingle();
        result.Events[0].Title.Should().Be("Конференция по новым компьютерным технологиям");
        cacheMock.Verify(x => x.GetStringAsync(It.IsAny<string>()), Times.Never);
        cacheMock.Verify(x => x.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "ChangeEvent")]
    public async Task ChangeEventAsync_Should_Update_Event_And_Invalidate_Cache()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var cacheMock = new Mock<ICacheService>();
        var service = new EventService(repositoryMock.Object, Mock.Of<ILogger<EventService>>(), cacheMock.Object, RedisOptions, FakeTime);
        var startAt = FakeTime.GetUtcNow().UtcDateTime.AddDays(1);
        var evt = Event.Create("Конференция", startAt, startAt.AddHours(2), 30, "Описание");
        var update = UpdateEventDto.Create(title: "Новая конференция", description: "Ежегодное мероприятие");

        repositoryMock.Setup(x => x.GetByIdAsync(evt.Id, It.IsAny<CancellationToken>())).ReturnsAsync(evt);
        repositoryMock.Setup(x => x.UpdateAsync(evt, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await service.ChangeEventAsync(evt.Id, update, CancellationToken.None);

        evt.Title.Should().Be("Новая конференция");
        repositoryMock.Verify(x => x.UpdateAsync(evt, It.IsAny<CancellationToken>()), Times.Once);
        cacheMock.Verify(x => x.RemoveAsync(KeysForEvents.ForEvent(evt.Id)), Times.Once);
    }

    [Fact]
    [Trait("Category", "ChangeEvent")]
    public async Task ChangeEventAsync_Should_Not_Invalidate_Cache_When_NotFound()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var cacheMock = new Mock<ICacheService>();
        var service = new EventService(repositoryMock.Object, Mock.Of<ILogger<EventService>>(), cacheMock.Object, RedisOptions, FakeTime);
        var eventId = Guid.NewGuid();
        var update = UpdateEventDto.Create(title: "Новая конференция");

        repositoryMock.Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync((Event?) null);

        Func<Task> act = () => service.ChangeEventAsync(eventId, update, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        cacheMock.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "CancelEvent")]
    public async Task CancelEventAsync_Should_Delete_Event_And_Invalidate_Cache()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var cacheMock = new Mock<ICacheService>();
        var service = new EventService(repositoryMock.Object, Mock.Of<ILogger<EventService>>(), cacheMock.Object, RedisOptions, FakeTime);
        var eventId = Guid.NewGuid();

        repositoryMock.Setup(x => x.DeleteAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await service.CancelEventAsync(eventId, CancellationToken.None);

        repositoryMock.Verify(x => x.DeleteAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);
        cacheMock.Verify(x => x.RemoveAsync(KeysForEvents.ForEvent(eventId)), Times.Once);
    }

    [Fact]
    [Trait("Category", "CancelEvent")]
    public async Task CancelEventAsync_Should_Not_Invalidate_Cache_When_NotFound()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var cacheMock = new Mock<ICacheService>();
        var service = new EventService(repositoryMock.Object, Mock.Of<ILogger<EventService>>(), cacheMock.Object, RedisOptions, FakeTime);
        var eventId = Guid.NewGuid();

        repositoryMock.Setup(x => x.DeleteAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Func<Task> act = () => service.CancelEventAsync(eventId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        repositoryMock.Verify(x => x.DeleteAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);
        cacheMock.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "ReleaseSeat")]
    public async Task ReleaseSeatAsync_Should_ReleaseSeat_And_Invalidate_Cache()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var cacheMock = new Mock<ICacheService>();
        var service = new EventService(repositoryMock.Object, Mock.Of<ILogger<EventService>>(), cacheMock.Object, RedisOptions, FakeTime);
        var startAt = FakeTime.GetUtcNow().UtcDateTime.AddDays(1);
        var evt = Event.Create("Конференция", startAt, startAt.AddHours(2), 20, "Описание");
        evt.TryReserveSeats();

        repositoryMock.Setup(x => x.GetByIdAsync(evt.Id, It.IsAny<CancellationToken>())).ReturnsAsync(evt);

        await service.ReleaseSeatAsync(evt.Id, CancellationToken.None);

        evt.AvailableSeats.Should().Be(20);
        repositoryMock.Verify(x => x.UpdateAsync(evt, It.IsAny<CancellationToken>()), Times.Once);
        cacheMock.Verify(x => x.RemoveAsync(KeysForEvents.ForEvent(evt.Id)), Times.Once);
    }

    [Fact]
    [Trait("Category", "ReleaseSeat")]
    public async Task ReleaseSeatAsync_Should_Not_Invalidate_Cache_When_NotFound()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var cacheMock = new Mock<ICacheService>();
        var service = new EventService(repositoryMock.Object, Mock.Of<ILogger<EventService>>(), cacheMock.Object, RedisOptions, FakeTime);
        var eventId = Guid.NewGuid();

        repositoryMock.Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync((Event?) null);

        Func<Task> act = () => service.ReleaseSeatAsync(eventId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        cacheMock.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Never);
    }


    [Fact]
    [Trait("Category", "Cache")]
    public async Task GetOrSetCacheAsync_Should_Only_Call_Repository_Once_During_Concurrent_Access()
    {
        // Arrange
        var repositoryMock = new Mock<IEventRepository>();
        var cacheMock = new Mock<ICacheService>();
        var service = new EventService(repositoryMock.Object, Mock.Of<ILogger<EventService>>(), cacheMock.Object, RedisOptions, FakeTime);

        var eventId = Guid.NewGuid();
        var cacheKey = KeysForEvents.ForEvent(eventId);
        var startAt = FakeTime.GetUtcNow().UtcDateTime.AddDays(1);
        var evt = Event.Create("Конкурентный тест", startAt, startAt.AddHours(2), 100, "Описание");

        string? cachedValue = null;
        cacheMock.Setup(x => x.GetStringAsync(cacheKey)).ReturnsAsync(() => cachedValue);
        cacheMock.Setup(x => x.SetStringAsync(cacheKey, It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Callback<string, string, TimeSpan>((_, val, _) => cachedValue = val);

        repositoryMock.Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(evt);

        // Act
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => service.GetEventAsync(eventId, CancellationToken.None));
        var results = await Task.WhenAll(tasks);

        // Assert
        repositoryMock.Verify(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);
        cacheMock.Verify(x => x.SetStringAsync(cacheKey, It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Once);
        results.Should().AllSatisfy(r => r.Id.Should().Be(evt.Id));
    }


}
