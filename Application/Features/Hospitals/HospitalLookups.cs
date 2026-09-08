namespace EGovServices.Application.Features.Hospitals;

/// <summary>
/// قاموس بسيط: القيمة الإنكليزية اللي يبعتها الفرونت (نفس "value" الموجودة
/// فعلياً بخيارات ServiceFormFields لهاي الخدمة — governorate/specialty)
/// → القيمة العربية المخزّنة فعلياً بعمودي Governorate/Specialty بجدول
/// Hospitals.
///
/// ⚠️ القيم هون منسوخة حرفياً من رد /api/services/{id}/schema الفعلي
/// (مو مخمّنة)، لأن الفرونت بياخد الخيارات من هناك مباشرة، فلازم القاموس
/// يطابقها بالحرف.
///
/// الاستخدام: GetHospitalsQuery يحاول يترجم القيمة الواردة؛ إذا ما لقاها
/// بالقاموس (يعني وصلت عربي أصلاً، أو "all_governorates"، أو قيمة غير
/// معروفة)، يرجّع نفس القيمة كما وصلت بدون تغيير.
/// </summary>
public static class HospitalLookups
{
    public static readonly Dictionary<string, string> GovernorateCodeToArabic = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Damascus"] = "دمشق",
        ["Rif Dimashq"] = "ريف دمشق",
        ["Aleppo"] = "حلب",
        ["Homs"] = "حمص",
        ["Hama"] = "حماه",              // ⚠️ label بالـschema "حماة" لكن القيمة الفعلية بجدول Hospitals "حماه" (بدون تاء مربوطة)
        ["Latakia"] = "اللاذقية",
        ["Tartus"] = "طرطوس",
        ["Idlib"] = "ادلب",             // ⚠️ label بالـschema "إدلب" لكن القيمة الفعلية بجدول Hospitals "ادلب" (بدون همزة)
        ["Daraa"] = "درعا",
        ["As-Suwayda"] = "السويداء",
        ["Quneitra"] = "القنيطرة",
        ["Deir ez-Zor"] = "دير الزور",
        ["Raqqa"] = "الرقة",
        ["Al-Hasakah"] = "الحسكة",
    };

    public static readonly Dictionary<string, string> SpecialtyCodeToArabic = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pediatrics"] = "أطفال",
        ["pediatrics_gynecology"] = "أطفال - نسائية",
        ["cardiology_surgery"] = "أمراض القلب وجراحتها",
        ["emergency"] = "اسعافي",
        ["addiction_psychiatry"] = "الإدمان والامراض النفسية",
        ["ophthalmology"] = "العيون",
        ["obstetrics_pediatrics"] = "توليد وأطفال",
        ["internal_medicine"] = "داخلية",
        ["infectious_diseases"] = "داخلية سارية",
        ["nephrology_dialysis"] = "زرع وغسيل الكلية وتنقية الدم",
        ["general"] = "عام",
        ["general_cardiology"] = "عام-قلبية",
        ["general_field"] = "عام/ميداني",
        ["neuropsychiatry"] = "عصبية نفسية",
        ["eye_specialty"] = "عينية",
        ["cardiology"] = "قلبية",
        ["kidney"] = "كلية",
        ["obstetrics_gynecology"] = "نسائية وتوليد",
        ["gynecology_pediatrics"] = "نسائية- اطفال",
    };

    /// <summary>يترجم قيمة إنكليزية إلى عربي؛ لو مش موجودة بالقاموس يرجّع القيمة كما هي (يدعم إرسال عربي مباشرة أو "all_governorates").</summary>
    public static string GovernorateToArabic(string value) =>
        GovernorateCodeToArabic.TryGetValue(value, out var arabic) ? arabic : value;

    public static string SpecialtyToArabic(string value) =>
        SpecialtyCodeToArabic.TryGetValue(value, out var arabic) ? arabic : value;
}
