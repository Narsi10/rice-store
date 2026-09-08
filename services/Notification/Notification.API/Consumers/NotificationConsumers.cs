using BuildingBlocks.IntegrationEvents;
using MassTransit;
using Notification.API.Services;

namespace Notification.API.Consumers;

/// <summary>Sends the order confirmation email when the saga confirms an order.</summary>
public class OrderConfirmedConsumer : IConsumer<OrderConfirmed>
{
    private readonly IEmailSender _email;
    public OrderConfirmedConsumer(IEmailSender email) => _email = email;

    public async Task Consume(ConsumeContext<OrderConfirmed> ctx)
    {
        var (subject, html) = EmailTemplates.Confirmed(ctx.Message.OrderId);
        await _email.SendAsync(ctx.Message.CustomerEmail, subject, html);
    }
}

/// <summary>Emails the customer when their order is cancelled by the saga.</summary>
public class OrderCancelledConsumer : IConsumer<OrderCancelled>
{
    private readonly IEmailSender _email;
    public OrderCancelledConsumer(IEmailSender email) => _email = email;

    public async Task Consume(ConsumeContext<OrderCancelled> ctx)
    {
        var (subject, html) = EmailTemplates.Cancelled(ctx.Message.OrderId, ctx.Message.Reason);
        await _email.SendAsync(ctx.Message.CustomerEmail, subject, html);
    }
}

/// <summary>Emails the customer when an admin approves their order.</summary>
public class OrderApprovedConsumer : IConsumer<OrderApproved>
{
    private readonly IEmailSender _email;
    public OrderApprovedConsumer(IEmailSender email) => _email = email;

    public async Task Consume(ConsumeContext<OrderApproved> ctx)
    {
        var (subject, html) = EmailTemplates.Approved(ctx.Message.OrderId);
        await _email.SendAsync(ctx.Message.CustomerEmail, subject, html);
    }
}

/// <summary>Emails the customer when an admin rejects their order.</summary>
public class OrderRejectedConsumer : IConsumer<OrderRejected>
{
    private readonly IEmailSender _email;
    public OrderRejectedConsumer(IEmailSender email) => _email = email;

    public async Task Consume(ConsumeContext<OrderRejected> ctx)
    {
        var (subject, html) = EmailTemplates.Rejected(ctx.Message.OrderId, ctx.Message.Reason);
        await _email.SendAsync(ctx.Message.CustomerEmail, subject, html);
    }
}
