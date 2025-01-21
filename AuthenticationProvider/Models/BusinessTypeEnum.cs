using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Models;

// This enum is fetched for dropdown menu in frontend
public enum BusinessTypeEnum
{
    [Display(Name = "Detaljhandel")]
    Retail,

    [Display(Name = "Tjänsteföretag")]
    Service,

    [Display(Name = "Tillverkning")]
    Manufacturing,

    [Display(Name = "IT")]
    IT,

    [Display(Name = "Vård och Hälsa")]
    Healthcare,

    [Display(Name = "Byggsektor")]
    Construction,

    [Display(Name = "Finans")]
    Finance,

    [Display(Name = "Fastigheter")]
    RealEstate,

    [Display(Name = "Transport")]
    Transportation,

    [Display(Name = "Jordbruk")]
    Agriculture,

    [Display(Name = "Utbildning")]
    Education,

    [Display(Name = "Hotell och Restaurang")]
    Hospitality,

    [Display(Name = "Turism")]
    Tourism,

    [Display(Name = "Underhållning")]
    Entertainment,

    [Display(Name = "Media")]
    Media,

    [Display(Name = "Telekommunikation")]
    Telecommunications,

    [Display(Name = "Energi")]
    Energy,

    [Display(Name = "Logistik")]
    Logistics,

    [Display(Name = "Juridik")]
    Legal,

    [Display(Name = "Konsulttjänster")]
    Consulting,

    [Display(Name = "Marknadsföring")]
    Marketing,

    [Display(Name = "Reklam")]
    Advertising,

    [Display(Name = "Livsmedel och Dryck")]
    FoodAndBeverage,

    [Display(Name = "Bilindustrin")]
    Automotive,

    [Display(Name = "Farmaceutiska produkter")]
    Pharmaceuticals,

    [Display(Name = "Engros")]
    Wholesale,

    [Display(Name = "Teknologi")]
    Technology,

    [Display(Name = "Programutveckling")]
    SoftwareDevelopment,

    [Display(Name = "E-handel")]
    ECommerce,

    [Display(Name = "Icke-vinstdrivande")]
    NonProfit,

    [Display(Name = "Försäkringar")]
    Insurance,

    [Display(Name = "Publicering")]
    Publishing,

    [Display(Name = "Luftfart")]
    Aerospace,

    [Display(Name = "Gruvdrift")]
    Mining,

    [Display(Name = "Textilier")]
    Textiles,

    [Display(Name = "Mode")]
    Fashion,

    [Display(Name = "Bioteknik")]
    Biotechnology,

    [Display(Name = "Arkitektur")]
    Architecture,

    [Display(Name = "Miljö- och hållbarhetstjänster")]
    EnvironmentalServices,

    [Display(Name = "Regering")]
    Government,

    [Display(Name = "Forskning och Utveckling")]
    ResearchAndDevelopment,

    [Display(Name = "Säkerhet")]
    Security,

    [Display(Name = "Eventhantering")]
    EventManagement,

    [Display(Name = "Sport och Fitness")]
    SportsAndFitness,

    [Display(Name = "Konst och Kultur")]
    ArtsAndCulture,

    [Display(Name = "Redovisning")]
    Accounting,

    [Display(Name = "Human Resources")]
    HumanResources,

    [Display(Name = "Supply Chain")]
    SupplyChain,

    [Display(Name = "Elektronik")]
    Electronics,

    [Display(Name = "Medicinteknik")]
    MedicalDevices,

    [Display(Name = "Städtjänster")]
    CleaningServices,

    [Display(Name = "Import och Export")]
    ImportExport,

    [Display(Name = "Byggutrustning")]
    ConstructionEquipment,

    [Display(Name = "Hemförbättring")]
    HomeImprovement,

    [Display(Name = "Apputveckling")]
    AppDevelopment,

    [Display(Name = "Design")]
    Design,

    [Display(Name = "Fotografi")]
    Photography,

    [Display(Name = "Landskapsvård")]
    Landscaping,

    [Display(Name = "Fastighetsförvaltning")]
    PropertyManagement,

    [Display(Name = "Digital Marknadsföring")]
    DigitalMarketing,

    [Display(Name = "Retail Tech")]
    RetailTech,

    [Display(Name = "Hälsa och Välmående")]
    HealthAndWellness
}
