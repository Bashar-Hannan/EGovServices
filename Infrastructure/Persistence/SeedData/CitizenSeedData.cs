using EGovServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Infrastructure.Persistence.SeedData;

public static class CitizenSeedData
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Citizen>().HasData(
            new Citizen
            {
                NationalNumber = "01100000003",
                FirstName = "ياسين",
                LastName = "الكردي",
                FatherName = "عمر",
                MotherName = "ليلى",
                Gender = "ذكر",
                Address = "اللاذقية - المشروع الأول",
                BirthDate = new DateOnly(2000, 1, 15),
                Email = "yassin@example.com"
            },
            new Citizen
            {
                NationalNumber = "01200000002",
                FirstName = "سارة",
                LastName = "الأحمد",
                FatherName = "محمود",
                MotherName = "مريم",
                Gender = "أنثى",
                Address = "دمشق - المزة",
                BirthDate = new DateOnly(1998, 11, 22),
                Email = "sara@example.com"
            },
            new Citizen
            {
                NationalNumber = "02100000001",
                FirstName = "أحمد",
                LastName = "المنصور",
                FatherName = "محمد",
                MotherName = "فاطمة",
                Gender = "ذكر",
                Address = "حلب - حي الفرقان",
                BirthDate = new DateOnly(1995, 5, 10),
                Email = "ahmed@example.com"
            },
            new Citizen
            {
                NationalNumber = "02250150972",
                FirstName = "بشار",
                LastName = "حنان",
                FatherName = "عماد",
                MotherName = "فريدة",
                Gender = "ذكر",
                Address = "حلب - الشيخ مقصود",
                BirthDate = new DateOnly(2002, 12, 29),
                Email = "basharhannan400@gmail.com"
            }
        );
    }
}