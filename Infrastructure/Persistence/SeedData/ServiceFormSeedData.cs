using EGovServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Infrastructure.Persistence.SeedData;

public static class ServiceFormSeedData
{
    private static readonly Guid EntityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ServiceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid MaritalFieldId = Guid.Parse("33333333-0001-0001-0001-000000000009");

    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GovernmentEntity>().HasData(new GovernmentEntity
        {
            Id = EntityId, Name = "وزارة الداخلية",
            Description = "Ministry of Interior", IsActive = true
        });

        modelBuilder.Entity<GovernmentService>().HasData(new GovernmentService
        {
            Id = ServiceId, GovernmentEntityId = EntityId,
            Name = "تجديد جواز السفر",
            Description = "خدمة تجديد جواز السفر منتهي الصلاحية",
            Requirements = "صورة شخصية + جواز السفر القديم + إثبات السكن",
            ServiceFee = 150.00m, IsActive = true
        });

        ServiceFormField[] fields =
        [
            new() { Id = Guid.Parse("33333333-0001-0001-0001-000000000001"), GovernmentServiceId = ServiceId,
                FieldName = "fullName", Label = "الاسم الكامل", FieldType = "text",
                IsRequired = true, DisplayOrder = 1,
                ValidationRules = """{"minLength":3,"maxLength":100}""",
                Placeholder = "أدخل اسمك الرباعي كما في الهوية",
                HelpText = "يجب أن يطابق الاسم المسجل في الهوية الوطنية", IsActive = true },

            new() { Id = Guid.Parse("33333333-0001-0001-0001-000000000002"), GovernmentServiceId = ServiceId,
                FieldName = "nationalNumber", Label = "رقم الهوية الوطنية", FieldType = "text",
                IsRequired = true, DisplayOrder = 2,
                ValidationRules = """{"length":10,"pattern":"^[0-9]{10}$"}""",
                Placeholder = "1234567890", IsActive = true },

            new() { Id = Guid.Parse("33333333-0001-0001-0001-000000000003"), GovernmentServiceId = ServiceId,
                FieldName = "currentPassportNumber", Label = "رقم جواز السفر الحالي", FieldType = "text",
                IsRequired = true, DisplayOrder = 3,
                ValidationRules = """{"pattern":"^[A-Z]{3}[0-9]{6}$","customMessage":"يجب أن يكون رقم الجواز بصيغة ABC123456"}""",
                Placeholder = "ABC123456", IsActive = true },

            new() { Id = Guid.Parse("33333333-0001-0001-0001-000000000004"), GovernmentServiceId = ServiceId,
                FieldName = "phoneNumber", Label = "رقم الجوال", FieldType = "tel",
                IsRequired = true, DisplayOrder = 4,
                ValidationRules = """{"pattern":"^05[0-9]{8}$"}""",
                Placeholder = "0501234567", IsActive = true },

            new() { Id = Guid.Parse("33333333-0001-0001-0001-000000000005"), GovernmentServiceId = ServiceId,
                FieldName = "email", Label = "البريد الإلكتروني", FieldType = "email",
                IsRequired = true, DisplayOrder = 5,
                ValidationRules = """{"pattern":"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"}""",
                Placeholder = "example@email.com", IsActive = true },

            new() { Id = Guid.Parse("33333333-0001-0001-0001-000000000006"), GovernmentServiceId = ServiceId,
                FieldName = "personalPhoto", Label = "صورة شخصية حديثة", FieldType = "file",
                IsRequired = true, DisplayOrder = 6,
                ValidationRules = """{"maxSize":5242880,"allowedTypes":["image/jpeg","image/png"]}""",
                HelpText = "صورة بخلفية بيضاء، مقاس 4×6 سم",
                Metadata = """{"accept":".jpg,.jpeg,.png","maxSizeMB":5}""", IsActive = true },

            new() { Id = Guid.Parse("33333333-0001-0001-0001-000000000007"), GovernmentServiceId = ServiceId,
                FieldName = "currentAddress", Label = "العنوان الحالي", FieldType = "textarea",
                IsRequired = true, DisplayOrder = 7,
                ValidationRules = """{"minLength":10,"maxLength":200}""",
                Placeholder = "أدخل عنوانك بالتفصيل", IsActive = true },

            new() { Id = Guid.Parse("33333333-0001-0001-0001-000000000008"), GovernmentServiceId = ServiceId,
                FieldName = "preferredPickupDate", Label = "تاريخ الاستلام المفضل", FieldType = "date",
                IsRequired = true, DisplayOrder = 8,
                ValidationRules = """{"minDate":"today+7","maxDate":"today+30"}""",
                HelpText = "سيتم إشعارك بالموعد النهائي خلال 48 ساعة", IsActive = true },

            new() { Id = MaritalFieldId, GovernmentServiceId = ServiceId,
                FieldName = "maritalStatus", Label = "الحالة الاجتماعية", FieldType = "select",
                IsRequired = true, DisplayOrder = 9, IsActive = true }
        ];

        modelBuilder.Entity<ServiceFormField>().HasData(fields);

        ServiceFieldOption[] options =
        [
            new() { Id = Guid.Parse("44444444-0001-0001-0001-000000000001"), ServiceFormFieldId = MaritalFieldId, OptionValue = "single", OptionLabel = "أعزب/عزباء", DisplayOrder = 1, IsActive = true },
            new() { Id = Guid.Parse("44444444-0001-0001-0001-000000000002"), ServiceFormFieldId = MaritalFieldId, OptionValue = "married", OptionLabel = "متزوج/متزوجة", DisplayOrder = 2, IsActive = true },
            new() { Id = Guid.Parse("44444444-0001-0001-0001-000000000003"), ServiceFormFieldId = MaritalFieldId, OptionValue = "divorced", OptionLabel = "مطلق/مطلقة", DisplayOrder = 3, IsActive = true },
            new() { Id = Guid.Parse("44444444-0001-0001-0001-000000000004"), ServiceFormFieldId = MaritalFieldId, OptionValue = "widowed", OptionLabel = "أرمل/أرملة", DisplayOrder = 4, IsActive = true }
        ];

        modelBuilder.Entity<ServiceFieldOption>().HasData(options);
    }
}
