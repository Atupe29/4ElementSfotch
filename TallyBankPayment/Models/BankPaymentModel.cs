using System.ComponentModel.DataAnnotations;

namespace TallyBankPayment.Models;

public class BankPaymentModel
{
    [Required] public string VoucherNumber { get; set; } = "";
    [Required] public string Date { get; set; } = ""; // YYYYMMDD
    [Required] public string VoucherType { get; set; } = "";
    [Required] public string BankAccount { get; set; } = "";
    [Required] public string PaymentMode { get; set; } = "";
    public string? InstrumentNo { get; set; }
    public string? InstrumentDate { get; set; } // YYYYMMDD
    [Required] public string PartyLedger { get; set; } = "";
    [Required] public string PartyName { get; set; } = "";
    [Required] public decimal Amount { get; set; }
    [Required] public string Narration { get; set; } = "";
    [Required] public string CostCenter { get; set; } = "";
    [Required] public string GstRegistrationType { get; set; } = "";
    [Required] public string StateName { get; set; } = "";
    [Required] public string PlaceOfSupply { get; set; } = "";
    [Required] public string BillNo { get; set; } = "";
    public string? BillDate { get; set; } // DD-MMM-YY
    [Required] public string EnteredBy { get; set; } = "";
    public string? PaymentFavouring { get; set; }
    public string? TransactionType { get; set; }
    public string? TransferMode { get; set; }
    public string? UniqueReference { get; set; }
}