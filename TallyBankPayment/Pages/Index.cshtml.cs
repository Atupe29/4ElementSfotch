using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using TallyBankPayment.Models;
using TallyBankPayment.Services;

namespace TallyBankPayment.Pages;

[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly TallyService _tally;

    public IndexModel(TallyService tally)
    {
        _tally = tally;
    }

    [BindProperty]
    public BankPaymentModel VoucherData { get; set; } = new();

    public string? XmlData { get; set; }
    public bool IsSuccess { get; set; }
    public string? ResponseMessage { get; set; }
    public bool ShowDetails { get; set; }
    public TallyResult? TallyResponse { get; set; }
    public string? TallyMessage { get; set; }

    public List<SelectListItem> VoucherTypeOptions { get; set; } = new();
    public List<SelectListItem> BankAccountOptions { get; set; } = new();
    public List<SelectListItem> PaymentModeOptions { get; set; } = new();
    public List<SelectListItem> GstRegistrationTypeOptions { get; set; } = new();

    public void OnGet()
    {
        BuildOptions();
        VoucherData = new BankPaymentModel
        {
            VoucherNumber = "",
            Date = DateTime.Now.ToString("yyyyMMdd"),
            VoucherType = "MUM BANK PAYMENT",
            BankAccount = "SBI MUMBAI-57615",
            PaymentMode = "IMPS",
            InstrumentNo = "",
            InstrumentDate = DateTime.Now.ToString("yyyyMMdd"),
            PartyLedger = "BEST UNDERTAKING-MUMBAI",
            PartyName = "SBI MUMBAI-57615",
            Amount = 0m,
            Narration = "BEING AMOUNT PAID AGAINST BILL NO-",
            CostCenter = "MUMBAI UNIT",
            GstRegistrationType = "Unregistered/Consumer",
            StateName = "Maharashtra",
            PlaceOfSupply = "Maharashtra",
            BillNo = "",
            BillDate = DateTime.Now.ToString("dd-MMM-yy", CultureInfo.InvariantCulture),
            EnteredBy = "anand patil",
            PaymentFavouring = "BEST UNDERTAKING-MUMBAI",
            TransactionType = "Inter Bank Transfer",
            TransferMode = "IMPS",
            UniqueReference = ""
        };
    }

    public async Task OnPostAsync()
    {
        BuildOptions();
        XmlData = _tally.GenerateBankPaymentXml(VoucherData);
        TallyResponse = await _tally.SendToTallyAsync(XmlData);

        if (TallyResponse.Success)
        {
            var body = TallyResponse.Response ?? "";
            if (TallyService.IsSuccessResponse(body))
            {
                IsSuccess = true;
                ResponseMessage = "Bank Payment Voucher successfully created in Tally!";
            }
            else
            {
                var (hasErr, msg) = TallyService.TryExtractError(body);
                IsSuccess = false;
                ResponseMessage = hasErr ? "Tally reported an error" : "Tally processed the request";
                TallyMessage = hasErr ? msg : body;
                ShowDetails = true;
            }
        }
        else
        {
            IsSuccess = false;
            ResponseMessage = $"Failed to connect to Tally (HTTP {TallyResponse.Status})";
            if (!string.IsNullOrWhiteSpace(TallyResponse.Error))
            {
                ResponseMessage += $" | Error: {TallyResponse.Error}";
            }
            ShowDetails = true;
        }
    }

    private void BuildOptions()
    {
        VoucherTypeOptions = new List<SelectListItem>
        {
            new("AUR BANK PAYMENT", "AUR BANK PAYMENT"),
            new("MUMBAI BANK PAYMENT", "MUM BANK PAYMENT"),
            new("Ako BANK PAYMENT", "DEL BANK PAYMENT"),
            new("CHEQUE PAYMENT", "CHEQUE PAYMENT"),
            new("CASH PAYMENT", "CASH PAYMENT"),
            new("NEFT PAYMENT", "NEFT PAYMENT"),
            new("RTGS PAYMENT", "RTGS PAYMENT"),
            new("IMPS PAYMENT", "IMPS PAYMENT"),
            new("UPI PAYMENT", "UPI PAYMENT"),
            new("DEMAND DRAFT PAYMENT", "DD PAYMENT")
        };

        BankAccountOptions = new List<SelectListItem>
        {
            new("HDFC Bank", "HDFC Bank"),
            new("SBI Bank", "SBI Bank"),
            new("ICICI Bank", "ICICI Bank"),
            new("SBI MUMBAI-57615", "SBI MUMBAI-57615")
        };

        PaymentModeOptions = new List<SelectListItem>
        {
            new("Cheque", "Cheque"),
            new("Demand Draft", "DD"),
            new("NEFT", "NEFT"),
            new("RTGS", "RTGS"),
            new("IMPS", "IMPS"),
            new("Transacted", "Transacted")
        };

        GstRegistrationTypeOptions = new List<SelectListItem>
        {
            new("Unregistered/Consumer", "Unregistered/Consumer"),
            new("Regular", "Regular"),
            new("Composition", "Composition")
        };
    }
}
