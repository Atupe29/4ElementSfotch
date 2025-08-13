using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using TallyBankPayment.Services;

namespace TallyBankPayment.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly ITallyClient _tallyClient;
    private readonly TallyOptions _tallyOptions;

    public IndexModel(ILogger<IndexModel> logger, ITallyClient tallyClient, IOptions<TallyOptions> tallyOptions)
    {
        _logger = logger;
        _tallyClient = tallyClient;
        _tallyOptions = tallyOptions.Value;
    }

    [BindProperty]
    public BankPaymentForm Form { get; set; } = new();

    public bool Submitted { get; private set; }
    public bool IsSuccess { get; private set; }
    public string ResponseMessage { get; private set; } = string.Empty;
    public bool ShowDetails { get; private set; }
    public int HttpStatus { get; private set; }
    public string TallyMessage { get; private set; } = string.Empty;
    public string ConnectionError { get; private set; } = string.Empty;
    public string XmlSent { get; private set; } = string.Empty;
    public string TallyRawResponse { get; private set; } = string.Empty;

    public void OnGet()
    {
        if (string.IsNullOrWhiteSpace(Form.Date))
        {
            Form.Date = DateTime.UtcNow.ToString("yyyyMMdd");
        }
        if (string.IsNullOrWhiteSpace(Form.InstrumentDate))
        {
            Form.InstrumentDate = DateTime.UtcNow.ToString("yyyyMMdd");
        }
        if (string.IsNullOrWhiteSpace(Form.BillDate))
        {
            Form.BillDate = DateTime.UtcNow.ToString("dd-MMM-yy", CultureInfo.InvariantCulture);
        }
        if (string.IsNullOrWhiteSpace(Form.Narration))
        {
            Form.Narration = "BEING AMOUNT PAID AGAINST BILL NO-";
        }
        if (string.IsNullOrWhiteSpace(Form.EnteredBy))
        {
            Form.EnteredBy = "anand patil";
        }
        if (string.IsNullOrWhiteSpace(Form.PartyLedger))
        {
            Form.PartyLedger = "BEST UNDERTAKING-MUMBAI";
        }
        if (string.IsNullOrWhiteSpace(Form.PartyName))
        {
            Form.PartyName = "SBI MUMBAI-57615";
        }
        if (string.IsNullOrWhiteSpace(Form.BankAccount))
        {
            Form.BankAccount = "SBI MUMBAI-57615";
        }
        if (string.IsNullOrWhiteSpace(Form.PaymentMode))
        {
            Form.PaymentMode = "IMPS";
        }
        if (string.IsNullOrWhiteSpace(Form.TransactionType))
        {
            Form.TransactionType = "Inter Bank Transfer";
        }
        if (string.IsNullOrWhiteSpace(Form.TransferMode))
        {
            Form.TransferMode = "IMPS";
        }
        if (string.IsNullOrWhiteSpace(Form.PaymentFavouring))
        {
            Form.PaymentFavouring = "BEST UNDERTAKING-MUMBAI";
        }
        if (string.IsNullOrWhiteSpace(Form.CostCenter))
        {
            Form.CostCenter = "MUMBAI UNIT";
        }
        if (string.IsNullOrWhiteSpace(Form.GstRegistrationType))
        {
            Form.GstRegistrationType = "Unregistered/Consumer";
        }
        if (string.IsNullOrWhiteSpace(Form.StateName))
        {
            Form.StateName = "Maharashtra";
        }
        if (string.IsNullOrWhiteSpace(Form.PlaceOfSupply))
        {
            Form.PlaceOfSupply = "Maharashtra";
        }
        if (string.IsNullOrWhiteSpace(Form.VoucherType))
        {
            Form.VoucherType = "MUM BANK PAYMENT";
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Submitted = true;

        var xml = GenerateBankPaymentXml(Form, _tallyOptions.Company);
        XmlSent = xml;

        try
        {
            var result = await _tallyClient.SendToTallyAsync(xml, _tallyOptions);
            HttpStatus = result.StatusCode;
            TallyRawResponse = result.ResponseText ?? string.Empty;

            if (result.Success)
            {
                var text = result.ResponseText ?? string.Empty;
                if (text.Contains("<CREATED>1</CREATED>", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("<SUCCESS>1</SUCCESS>", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("successfully", StringComparison.OrdinalIgnoreCase))
                {
                    IsSuccess = true;
                    ResponseMessage = "Bank Payment Voucher successfully created in Tally!";
                }
                else if (text.Contains("<ERROR>", StringComparison.OrdinalIgnoreCase))
                {
                    ResponseMessage = "Tally reported an error";
                    TallyMessage = ExtractBetween(text, "<ERROR>", "</ERROR>") ?? text;
                    ShowDetails = true;
                }
                else
                {
                    ResponseMessage = "Tally processed the request";
                    TallyMessage = text;
                    ShowDetails = true;
                }
            }
            else
            {
                ResponseMessage = $"Failed to connect to Tally (HTTP {result.StatusCode})";
                ConnectionError = result.ErrorMessage ?? string.Empty;
                ShowDetails = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting to Tally");
            IsSuccess = false;
            ResponseMessage = "Unexpected error while sending to Tally";
            ConnectionError = ex.Message;
            ShowDetails = true;
        }

        return Page();
    }

    private static string? ExtractBetween(string input, string start, string end)
    {
        var startIndex = input.IndexOf(start, StringComparison.OrdinalIgnoreCase);
        if (startIndex < 0) return null;
        startIndex += start.Length;
        var endIndex = input.IndexOf(end, startIndex, StringComparison.OrdinalIgnoreCase);
        if (endIndex < 0) return null;
        return input.Substring(startIndex, endIndex - startIndex);
    }

    private static string GenerateBankPaymentXml(BankPaymentForm data, string company)
    {
        string AmountString(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\"?>");
        sb.Append("<ENVELOPE>");
        sb.Append("<HEADER><TALLYREQUEST>Import Data</TALLYREQUEST></HEADER>");
        sb.Append("<BODY>");
        sb.Append("<IMPORTDATA>");
        sb.Append("<REQUESTDESC><REPORTNAME>Vouchers</REPORTNAME><STATICVARIABLES><SVCURRENTCOMPANY>");
        sb.Append(System.Security.SecurityElement.Escape(company));
        sb.Append("</SVCURRENTCOMPANY></STATICVARIABLES></REQUESTDESC>");
        sb.Append("<REQUESTDATA>");
        sb.Append("<TALLYMESSAGE xmlns:UDF=\"TallyUDF\">");
        sb.Append($"<VOUCHER REMOTEID=\"{XmlEscape(data.VoucherNumber)}\" VCHTYPE=\"{XmlEscape(data.VoucherType)}\" ACTION=\"Create\">");
        sb.Append($"<DATE>{XmlEscape(data.Date)}</DATE>");
        sb.Append($"<GSTREGISTRATIONTYPE>{XmlEscape(data.GstRegistrationType)}</GSTREGISTRATIONTYPE>");
        sb.Append($"<STATENAME>{XmlEscape(data.StateName)}</STATENAME>");
        sb.Append($"<NARRATION>{XmlEscape(data.Narration)}</NARRATION>");
        sb.Append($"<ENTEREDBY>{XmlEscape(data.EnteredBy)}</ENTEREDBY>");
        sb.Append("<COUNTRYOFRESIDENCE>India</COUNTRYOFRESIDENCE>");
        sb.Append($"<PLACEOFSUPPLY>{XmlEscape(data.PlaceOfSupply)}</PLACEOFSUPPLY>");
        sb.Append($"<VOUCHERTYPENAME>{XmlEscape(data.VoucherType)}</VOUCHERTYPENAME>");
        sb.Append($"<PARTYNAME>{XmlEscape(data.PartyName)}</PARTYNAME>");
        sb.Append($"<PARTYLEDGERNAME>{XmlEscape(data.PartyLedger)}</PARTYLEDGERNAME>");
        sb.Append($"<VOUCHERNUMBER>{XmlEscape(data.VoucherNumber)}</VOUCHERNUMBER>");
        sb.Append("<CMPGSTREGISTRATIONTYPE>Regular</CMPGSTREGISTRATIONTYPE>");
        sb.Append($"<PARTYMAILINGNAME>{XmlEscape(data.PartyName)}</PARTYMAILINGNAME>");
        sb.Append($"<CMPGSTSTATE>{XmlEscape(data.StateName)}</CMPGSTSTATE>");
        sb.Append($"<COSTCENTRENAME>{XmlEscape(data.CostCenter)}</COSTCENTRENAME>");

        // Party Entry (Debit)
        sb.Append("<ALLLEDGERENTRIES.LIST>");
        sb.Append($"<LEDGERNAME>{XmlEscape(data.PartyLedger)}</LEDGERNAME>");
        sb.Append("<ISDEEMEDPOSITIVE>Yes</ISDEEMEDPOSITIVE>");
        sb.Append($"<AMOUNT>-{AmountString(data.Amount)}</AMOUNT>");
        sb.Append("<BILLALLOCATIONS.LIST>");
        sb.Append($"<NAME>{XmlEscape(data.BillNo)}</NAME>");
        sb.Append($"<BILLCREDITPERIOD>{XmlEscape(data.BillDate)}</BILLCREDITPERIOD>");
        sb.Append("<BILLTYPE>Agst Ref</BILLTYPE>");
        sb.Append($"<AMOUNT>-{AmountString(data.Amount)}</AMOUNT>");
        sb.Append("</BILLALLOCATIONS.LIST>");
        sb.Append("</ALLLEDGERENTRIES.LIST>");

        // Bank Entry (Credit)
        sb.Append("<ALLLEDGERENTRIES.LIST>");
        sb.Append($"<LEDGERNAME>{XmlEscape(data.BankAccount)}</LEDGERNAME>");
        sb.Append("<ISDEEMEDPOSITIVE>No</ISDEEMEDPOSITIVE>");
        sb.Append($"<AMOUNT>{AmountString(data.Amount)}</AMOUNT>");

        // Payment Mode Details
        sb.Append("<BANKALLOCATIONS.LIST>");
        sb.Append($"<DATE>{XmlEscape(data.Date)}</DATE>");
        sb.Append($"<INSTRUMENTDATE>{XmlEscape(data.InstrumentDate)}</INSTRUMENTDATE>");
        sb.Append($"<TRANSACTIONTYPE>{XmlEscape(data.TransactionType)}</TRANSACTIONTYPE>");
        sb.Append($"<PAYMENTFAVOURING>{XmlEscape(data.PaymentFavouring)}</PAYMENTFAVOURING>");
        sb.Append($"<TRANSFERMODE>{XmlEscape(data.TransferMode)}</TRANSFERMODE>");
        sb.Append($"<INSTRUMENTNUMBER>{XmlEscape(data.InstrumentNo)}</INSTRUMENTNUMBER>");
        sb.Append($"<UNIQUEREFERENCENUMBER>{XmlEscape(data.UniqueReference)}</UNIQUEREFERENCENUMBER>");
        sb.Append($"<PAYMENTMODE>{XmlEscape(data.PaymentMode)}</PAYMENTMODE>");
        sb.Append($"<AMOUNT>{AmountString(data.Amount)}</AMOUNT>");
        sb.Append("</BANKALLOCATIONS.LIST>");
        sb.Append("</ALLLEDGERENTRIES.LIST>");

        sb.Append("</VOUCHER>");
        sb.Append("</TALLYMESSAGE>");
        sb.Append("</REQUESTDATA>");
        sb.Append("</IMPORTDATA>");
        sb.Append("</BODY>");
        sb.Append("</ENVELOPE>");
        return sb.ToString();
    }

    private static string XmlEscape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return System.Security.SecurityElement.Escape(value);
    }
}

public class BankPaymentForm
{
    public string VoucherNumber { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty; // YYYYMMDD
    public string VoucherType { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public string PaymentMode { get; set; } = string.Empty;
    public string? InstrumentNo { get; set; }
    public string InstrumentDate { get; set; } = string.Empty; // YYYYMMDD
    public string PartyLedger { get; set; } = string.Empty;
    public string PartyName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Narration { get; set; } = string.Empty;
    public string CostCenter { get; set; } = string.Empty;
    public string GstRegistrationType { get; set; } = string.Empty;
    public string StateName { get; set; } = string.Empty;
    public string PlaceOfSupply { get; set; } = string.Empty;
    public string BillNo { get; set; } = string.Empty;
    public string BillDate { get; set; } = string.Empty; // DD-MMM-YY
    public string EnteredBy { get; set; } = string.Empty;
    public string PaymentFavouring { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string TransferMode { get; set; } = string.Empty;
    public string? UniqueReference { get; set; }
}
