using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using TallyBankPayment.Models;

namespace TallyBankPayment.Services;

public class TallyConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 9000;
    public string Company { get; set; } = "";
}

public class TallyResult
{
    public bool Success { get; set; }
    public string Response { get; set; } = "";
    public int Status { get; set; }
    public string Error { get; set; } = "";
}

public class TallyService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TallyConfig _config;

    public TallyService(IHttpClientFactory httpClientFactory, TallyConfig config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    public async Task<TallyResult> SendToTallyAsync(string xml)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"http://{_config.Host}:{_config.Port}/tally/";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            request.Content = new StringContent(xml, Encoding.UTF8, "application/xml");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var response = await client.SendAsync(request, cts.Token);
            var body = await response.Content.ReadAsStringAsync(cts.Token);

            return new TallyResult
            {
                Success = response.IsSuccessStatusCode,
                Response = body,
                Status = (int)response.StatusCode,
                Error = ""
            };
        }
        catch (Exception ex)
        {
            return new TallyResult
            {
                Success = false,
                Response = "",
                Status = 0,
                Error = ex.Message
            };
        }
    }

    public string GenerateBankPaymentXml(BankPaymentModel d)
    {
        string amt = d.Amount.ToString("0.00", CultureInfo.InvariantCulture);
        string negAmt = "-" + amt;

        var sb = new StringBuilder();
        sb.Append(@"<?xml version=""1.0""?>");
        sb.Append("<ENVELOPE>");
        sb.Append("<HEADER><TALLYREQUEST>Import Data</TALLYREQUEST></HEADER>");
        sb.Append("<BODY>");
        sb.Append("<IMPORTDATA>");
        sb.Append("<REQUESTDESC><REPORTNAME>Vouchers</REPORTNAME><STATICVARIABLES>");
        sb.Append("<SVCURRENTCOMPANY>").Append(XmlEscape(_config.Company)).Append("</SVCURRENTCOMPANY>");
        sb.Append("</STATICVARIABLES></REQUESTDESC>");
        sb.Append("<REQUESTDATA>");

        sb.Append(@"<TALLYMESSAGE xmlns:UDF=""TallyUDF"">");
        sb.Append("<VOUCHER REMOTEID=\"").Append(XmlEscape(d.VoucherNumber))
          .Append("\" VCHTYPE=\"").Append(XmlEscape(d.VoucherType)).Append("\" ACTION=\"Create\">");
        sb.Append("<DATE>").Append(XmlEscape(d.Date)).Append("</DATE>");
        sb.Append("<GSTREGISTRATIONTYPE>").Append(XmlEscape(d.GstRegistrationType)).Append("</GSTREGISTRATIONTYPE>");
        sb.Append("<STATENAME>").Append(XmlEscape(d.StateName)).Append("</STATENAME>");
        sb.Append("<NARRATION>").Append(XmlEscape(d.Narration)).Append("</NARRATION>");
        sb.Append("<ENTEREDBY>").Append(XmlEscape(d.EnteredBy)).Append("</ENTEREDBY>");
        sb.Append("<COUNTRYOFRESIDENCE>India</COUNTRYOFRESIDENCE>");
        sb.Append("<PLACEOFSUPPLY>").Append(XmlEscape(d.PlaceOfSupply)).Append("</PLACEOFSUPPLY>");
        sb.Append("<VOUCHERTYPENAME>").Append(XmlEscape(d.VoucherType)).Append("</VOUCHERTYPENAME>");
        sb.Append("<PARTYNAME>").Append(XmlEscape(d.PartyName)).Append("</PARTYNAME>");
        sb.Append("<PARTYLEDGERNAME>").Append(XmlEscape(d.PartyLedger)).Append("</PARTYLEDGERNAME>");
        sb.Append("<VOUCHERNUMBER>").Append(XmlEscape(d.VoucherNumber)).Append("</VOUCHERNUMBER>");
        sb.Append("<CMPGSTREGISTRATIONTYPE>Regular</CMPGSTREGISTRATIONTYPE>");
        sb.Append("<PARTYMAILINGNAME>").Append(XmlEscape(d.PartyName)).Append("</PARTYMAILINGNAME>");
        sb.Append("<CMPGSTSTATE>").Append(XmlEscape(d.StateName)).Append("</CMPGSTSTATE>");
        sb.Append("<COSTCENTRENAME>").Append(XmlEscape(d.CostCenter)).Append("</COSTCENTRENAME>");

        // Party Entry (Debit)
        sb.Append("<ALLLEDGERENTRIES.LIST>");
        sb.Append("<LEDGERNAME>").Append(XmlEscape(d.PartyLedger)).Append("</LEDGERNAME>");
        sb.Append("<ISDEEMEDPOSITIVE>Yes</ISDEEMEDPOSITIVE>");
        sb.Append("<AMOUNT>").Append(negAmt).Append("</AMOUNT>");
        sb.Append("<BILLALLOCATIONS.LIST>");
        sb.Append("<NAME>").Append(XmlEscape(d.BillNo)).Append("</NAME>");
        sb.Append("<BILLCREDITPERIOD>").Append(XmlEscape(d.BillDate ?? "")).Append("</BILLCREDITPERIOD>");
        sb.Append("<BILLTYPE>Agst Ref</BILLTYPE>");
        sb.Append("<AMOUNT>").Append(negAmt).Append("</AMOUNT>");
        sb.Append("</BILLALLOCATIONS.LIST>");
        sb.Append("</ALLLEDGERENTRIES.LIST>");

        // Bank Entry (Credit)
        sb.Append("<ALLLEDGERENTRIES.LIST>");
        sb.Append("<LEDGERNAME>").Append(XmlEscape(d.BankAccount)).Append("</LEDGERNAME>");
        sb.Append("<ISDEEMEDPOSITIVE>No</ISDEEMEDPOSITIVE>");
        sb.Append("<AMOUNT>").Append(amt).Append("</AMOUNT>");

        // Payment Mode Details
        sb.Append("<BANKALLOCATIONS.LIST>");
        sb.Append("<DATE>").Append(XmlEscape(d.Date)).Append("</DATE>");
        sb.Append("<INSTRUMENTDATE>").Append(XmlEscape(d.InstrumentDate ?? "")).Append("</INSTRUMENTDATE>");
        sb.Append("<TRANSACTIONTYPE>").Append(XmlEscape(d.TransactionType ?? "")).Append("</TRANSACTIONTYPE>");
        sb.Append("<PAYMENTFAVOURING>").Append(XmlEscape(d.PaymentFavouring ?? "")).Append("</PAYMENTFAVOURING>");
        sb.Append("<TRANSFERMODE>").Append(XmlEscape(d.TransferMode ?? "")).Append("</TRANSFERMODE>");
        sb.Append("<INSTRUMENTNUMBER>").Append(XmlEscape(d.InstrumentNo ?? "")).Append("</INSTRUMENTNUMBER>");
        sb.Append("<UNIQUEREFERENCENUMBER>").Append(XmlEscape(d.UniqueReference ?? "")).Append("</UNIQUEREFERENCENUMBER>");
        sb.Append("<PAYMENTMODE>").Append(XmlEscape(d.PaymentMode)).Append("</PAYMENTMODE>");
        sb.Append("<AMOUNT>").Append(amt).Append("</AMOUNT>");
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

    public static bool IsSuccessResponse(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody)) return false;
        if (Regex.IsMatch(responseBody, @"<CREATED>\s*1\s*</CREATED>", RegexOptions.IgnoreCase)) return true;
        if (Regex.IsMatch(responseBody, @"<SUCCESS>\s*1\s*</SUCCESS>", RegexOptions.IgnoreCase)) return true;
        if (responseBody.Contains("successfully", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    public static (bool HasError, string Message) TryExtractError(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody)) return (false, "");
        var m = Regex.Match(responseBody, @"<ERROR>(.*?)</ERROR>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (m.Success) return (true, m.Groups[1].Value.Trim());
        if (responseBody.Contains("error", StringComparison.OrdinalIgnoreCase)) return (true, responseBody.Trim());
        return (false, "");
    }

    private static string XmlEscape(string? input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}