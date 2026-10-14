namespace DigitalServices.Application.Notifications;

public interface IOrderNotificationService
{
    Task NotifyOrderPaidAsync(OrderPaidNotification notification, CancellationToken cancellationToken = default);
}
