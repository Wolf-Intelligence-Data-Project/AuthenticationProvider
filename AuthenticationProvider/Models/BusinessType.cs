using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models;

public enum BusinessType
{
    [Display(Name = "")]
    Unspecified = 0, // Before typing or choosing the type

    [Display(Name = "Detaljhandel")]
    Retail = 1,

    [Display(Name = "Tjänsteföretag")]
    Service = 2,

    [Display(Name = "Tillverkning")]
    Manufacturing = 3,

    [Display(Name = "IT")]
    IT = 4,

    [Display(Name = "Vård och Hälsa")]
    Healthcare = 5,

    [Display(Name = "Byggsektor")]
    Construction = 6,

    [Display(Name = "Finans")]
    Finance = 7,

    [Display(Name = "Fastigheter")]
    RealEstate = 8,

    [Display(Name = "Transport")]
    Transportation = 9,

    [Display(Name = "Jordbruk")]
    Agriculture = 10,

    [Display(Name = "Utbildning")]
    Education = 11,

    [Display(Name = "Hotell och Restaurang")]
    Hospitality = 12,

    [Display(Name = "Turism")]
    Tourism = 13,

    [Display(Name = "Underhållning")]
    Entertainment = 14,

    [Display(Name = "Media")]
    Media = 15,

    [Display(Name = "Telekommunikation")]
    Telecommunications = 16,

    [Display(Name = "Energi")]
    Energy = 17,

    [Display(Name = "Logistik")]
    Logistics = 18,

    [Display(Name = "Juridik")]
    Legal = 19,

    [Display(Name = "Konsulttjänster")]
    Consulting = 20,

    [Display(Name = "Marknadsföring")]
    Marketing = 21,

    [Display(Name = "Reklam")]
    Advertising = 22,

    [Display(Name = "Livsmedel och Dryck")]
    FoodAndBeverage = 23,

    [Display(Name = "Bilindustrin")]
    Automotive = 24,

    [Display(Name = "Farmaceutiska produkter")]
    Pharmaceuticals = 25,

    [Display(Name = "Engros")]
    Wholesale = 26,

    [Display(Name = "Teknologi")]
    Technology = 27,

    [Display(Name = "Programutveckling")]
    SoftwareDevelopment = 28,

    [Display(Name = "E-handel")]
    ECommerce = 29,

    [Display(Name = "Icke-vinstdrivande")]
    NonProfit = 30,

    [Display(Name = "Försäkringar")]
    Insurance = 31,

    [Display(Name = "Publicering")]
    Publishing = 32,

    [Display(Name = "Luftfart")]
    Aerospace = 33,

    [Display(Name = "Gruvdrift")]
    Mining = 34,

    [Display(Name = "Textilier")]
    Textiles = 35,

    [Display(Name = "Mode")]
    Fashion = 36,

    [Display(Name = "Bioteknik")]
    Biotechnology = 37,

    [Display(Name = "Arkitektur")]
    Architecture = 38,

    [Display(Name = "Miljö- och hållbarhetstjänster")]
    EnvironmentalServices = 39,

    [Display(Name = "Regering")]
    Government = 40,

    [Display(Name = "Forskning och Utveckling")]
    ResearchAndDevelopment = 41,

    [Display(Name = "Säkerhet")]
    Security = 42,

    [Display(Name = "Eventhantering")]
    EventManagement = 43,

    [Display(Name = "Sport och Fitness")]
    SportsAndFitness = 44,

    [Display(Name = "Konst och Kultur")]
    ArtsAndCulture = 45,

    [Display(Name = "Redovisning")]
    Accounting = 46,

    [Display(Name = "Human Resources")]
    HumanResources = 47,

    [Display(Name = "Supply Chain")]
    SupplyChain = 48,

    [Display(Name = "Elektronik")]
    Electronics = 49,

    [Display(Name = "Medicinteknik")]
    MedicalDevices = 50,

    [Display(Name = "Städtjänster")]
    CleaningServices = 51,

    [Display(Name = "Import och Export")]
    ImportExport = 52,

    [Display(Name = "Byggutrustning")]
    ConstructionEquipment = 53,

    [Display(Name = "Hemförbättring")]
    HomeImprovement = 54,

    [Display(Name = "Apputveckling")]
    AppDevelopment = 55,

    [Display(Name = "Design")]
    Design = 56,

    [Display(Name = "Fotografi")]
    Photography = 57,

    [Display(Name = "Landskapsvård")]
    Landscaping = 58,

    [Display(Name = "Fastighetsförvaltning")]
    PropertyManagement = 59,

    [Display(Name = "Digital Marknadsföring")]
    DigitalMarketing = 60,

    [Display(Name = "Retail Tech")]
    RetailTech = 61,

    [Display(Name = "Hälsa och Välmående")]
    HealthAndWellness = 62
}

