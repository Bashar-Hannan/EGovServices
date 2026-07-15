namespace EGovServices.Application.Common.Interfaces;

/// <summary>
/// Marker interface — أي Command يمثّل "تأكيد دفع طلب" يُطبّقه.
/// PaymentProcessingBehavior يلتقطه تلقائياً، يخصم من المحفظة، ويغيّر الحالة.
///
/// لا يحمل UserId — يُستخرج من JWT داخل الـ Behavior مباشرة،
/// بنفس الطريقة المستخدمة في PayViolationHandler.
/// </summary>
public interface IRequiresPaymentCommand
{
    Guid ServiceRequestId { get; }
}
