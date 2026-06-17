namespace EGovServices.Domain.Enums;

public enum ServiceType
{
    /// <summary>
    /// خدمة رقمية بالكامل — تُعالج إلكترونياً وتُسلَّم فورياً
    /// مثال: لا حكم عليه، إخراج قيد
    /// </summary>
    Digital = 0,

    /// <summary>
    /// خدمة تحتاج حضوراً مادياً — يُرفع الملف ويُحجز موعد
    /// مثال: تجديد جواز السفر
    /// </summary>
    Appointment = 1
}
