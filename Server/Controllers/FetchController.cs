using Going.Plaid;
using Going.Plaid.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PlaidQuickstartBlazor.Shared;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace PlaidQuickstartBlazor.Server.Controllers;

[ApiController]
[Route("[controller]/[action]")]
[Produces("application/json")]
public class FetchController : ControllerBase
{
    private readonly ILogger<FetchController> _logger;
    private readonly PlaidCredentials _credentials;
    private readonly PlaidClient _client;

    public FetchController(ILogger<FetchController> logger, IOptions<PlaidCredentials> credentials, PlaidClient client)
    {
        _logger = logger;
        _credentials = credentials.Value;
        _client = client;
        _client.AccessToken = _credentials.AccessToken;
    }

    private DataTable SampleResult => new()
    {
        Columns = (new[] { "A", "B", "C", "D", "E" })
            .Select(x => new Column() { Title = x })
            .ToArray(),

        Rows = new[]
            {
                new Row() { Cells = new[] { "1", "2", "3", "4", "5" } },
                new Row() { Cells = new[] { "1", "2", "3", "4", "5" } },
                new Row() { Cells = new[] { "1", "2", "3", "4", "5" } },
                new Row() { Cells = new[] { "1", "2", "3", "4", "5" } },
            }
    };

    [HttpGet]
    [ProducesResponseType(typeof(DataTable), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Auth()
    {
        var request = new Going.Plaid.Auth.AuthGetRequest();

        var response = await _client.AuthGetAsync(request);

        if (response.Error is not null)
            return Error(response.Error);

        Account? AccountFor(string? id) => response.Accounts.Where(x => x.AccountId == id).SingleOrDefault();

        var result = new DataTable("Name", "Balance/r", "Account #", "Routing #")
        {
            Rows = response.Numbers.Ach
                .Select(x =>
                    new Row(
                        AccountFor(x.AccountId)?.Name ?? String.Empty,
                        AccountFor(x.AccountId)?.Balances?.Current?.ToString("C2") ?? string.Empty,
                        x.Account,
                        x.Routing
                    )
                )
                .ToArray()
        };

        return Ok(result);
    }
    [HttpGet]
    [ProducesResponseType(typeof(DataTable), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Transactions()
    {
        var request = new Going.Plaid.Transactions.TransactionsGetRequest()
        {
            Options = new TransactionsGetRequestOptions()
            {
                Count = 100
            },
            StartDate = DateOnly.FromDateTime( DateTime.Now - TimeSpan.FromDays(30) ),
            EndDate = DateOnly.FromDateTime(DateTime.Now)
        };

        var response = await _client.TransactionsGetAsync(request);

        if (response.Error is not null)
            return Error(response.Error);

        var result = new DataTable("Name", "Amount/r", "Date/r", "Category", "Channel")
        {
            Rows = response.Transactions
                .Select(x =>
                    new Row(
                        x.Name,
                        x.Amount.ToString("C2"),
                        x.Date.ToShortDateString(),
                        string.Join(':',x.Category ?? Enumerable.Empty<string>() ),
                        x.PaymentChannel.ToString()
                    )
                )
                .ToArray()
        };

        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(DataTable), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Identity()
    {
        var request = new Going.Plaid.Identity.IdentityGetRequest();

        var response = await _client.IdentityGetAsync(request);

        if (response.Error is not null)
            return Error(response.Error);

        var result = new DataTable("Names", "Emails", "Phone Numbers", "Addresses")
        {
            Rows = response.Accounts
                .SelectMany(a => 
                    a.Owners
                        .Select(o => 
                            new Row(
                                string.Join(", ", o.Names),
                                string.Join(", ", o.Emails.Select(x => x.Data)),
                                string.Join(", ", o.PhoneNumbers.Select(x => x.Data)),
                                string.Join(", ", o.Addresses.Select(x => x.Data.Street))
                            )
                        )
                ).ToArray()
        };

        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(DataTable), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Holdings()
    {
        var request = new Going.Plaid.Investments.InvestmentsHoldingsGetRequest();

        var response = await _client.InvestmentsHoldingsGetAsync(request);

        if (response.Error is not null)
            return Error(response.Error);

        Security? SecurityFor(string? id) => response?.Securities.Where(x => x.SecurityId == id).SingleOrDefault();
        Account? AccountFor(string? id) => response?.Accounts.Where(x => x.AccountId == id).SingleOrDefault();

        var result = new DataTable("Mask", "Name", "Quantity/r", "Close Price/r", "Value/r")
        {
            Rows = response.Holdings
            .Select(x =>
                new Row(
                    AccountFor(x.AccountId)?.Mask ?? string.Empty,
                    SecurityFor(x.SecurityId)?.Name ?? string.Empty,
                    x.Quantity.ToString("0.000"),
                    x.InstitutionPrice.ToString("C2"),
                    x.InstitutionValue.ToString("C2")
                )
            )
            .ToArray()
        };

        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(DataTable), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Investments_Transactions()
    {
        var request = new Going.Plaid.Investments.InvestmentsTransactionsGetRequest()
        {
            Options = new InvestmentsTransactionsGetRequestOptions()
            {
                Count = 100
            },
            StartDate = DateOnly.FromDateTime( DateTime.Now - TimeSpan.FromDays(30) ),
            EndDate = DateOnly.FromDateTime(DateTime.Now)
        };

        var response = await _client.InvestmentsTransactionsGetAsync(request);

        if (response.Error is not null)
            return Error(response.Error);

        Security? SecurityFor(string? id) => response?.Securities.Where(x => x.SecurityId == id).SingleOrDefault();

        var result = new DataTable("Name", "Amount/r", "Date/r", "Ticker")
        {
            Rows = response.InvestmentTransactions
            .Select(x =>
                new Row(
                    x.Name,
                    x.Amount.ToString("C2"),
                    x.Date.ToShortDateString(),
                    SecurityFor(x.SecurityId)?.TickerSymbol ?? string.Empty
                )
            )
            .ToArray()
        };

        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(DataTable), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Balance()
    {
        var request = new Going.Plaid.Accounts.AccountsBalanceGetRequest();

        var response = await _client.AccountsBalanceGetAsync(request);

        if (response.Error is not null)
            return Error(response.Error);

        var result = new DataTable("Name", "AccountId", "Balance/r")
        {
            Rows = response.Accounts
                .Select(x =>
                    new Row(
                        x.Name,
                        x.AccountId,
                        x.Balances?.Current?.ToString("C2") ?? string.Empty
                    )
                )
                .ToArray()
        };

        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(DataTable), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Accounts()
    {
        var request = new Going.Plaid.Accounts.AccountsGetRequest();

        var response = await _client.AccountsGetAsync(request);

        if (response.Error is not null)
            return Error(response.Error);

        var result = new DataTable("Name", "Balance/r", "Subtype", "Mask")
        {
            Rows = response.Accounts
                .Select(x =>
                    new Row(
                        x.Name,
                        x.Balances?.Current?.ToString("C2") ?? string.Empty,
                        x.Subtype?.ToString() ?? string.Empty,
                        x.Mask ?? string.Empty
                    )
                )
                .ToArray()
        };

        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(DataTable), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Item()
    {
        var request = new Going.Plaid.Item.ItemGetRequest();
        var response = await _client.ItemGetAsync(request);

        if (response.Error is not null)
            return Error(response.Error);

        _client.AccessToken = null;
        var intstrequest = new Going.Plaid.Institutions.InstitutionsGetByIdRequest() { InstitutionId = response.Item!.InstitutionId!, CountryCodes = new[] { CountryCode.Us } };
        var instresponse = await _client.InstitutionsGetByIdAsync(intstrequest);

        if (response.Error is not null)
            return Error(response.Error);

        var result = new DataTable("Institution Name", "Billed Products", "Available Products")
        {
            Rows = new[] 
            {
                new Row(
                    instresponse.Institution.Name,
                    string.Join(",",response.Item.BilledProducts.Select(x=>x.ToString())),
                    string.Join(",",response.Item.AvailableProducts.Select(x=>x.ToString()))
                )
            }
        };

        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(DataTable), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Liabilities()
    {
        var request = new Going.Plaid.Liabilities.LiabilitiesGetRequest();

        var response = await _client.LiabilitiesGetAsync(request);

        if (response.Error is not null)
            return Error(response.Error);

        Account? AccountFor(string? id) => response.Accounts.Where(x => x.AccountId == id).SingleOrDefault();

        var result = new DataTable("Type", "Account", "Balance/r")
        {
            Rows = response.Liabilities!.Credit!
                .Select(x =>
                    new Row(
                        "Credit",
                        AccountFor(x.AccountId)?.Name ?? string.Empty,
                        x.LastStatementBalance?.ToString("C2") ?? string.Empty
                    )
                )
                .Concat(
                    response.Liabilities!.Student!
                        .Select(x=>
                            new Row(
                                "Student Loan",
                                AccountFor(x.AccountId)?.Name ?? string.Empty,
                                AccountFor(x.AccountId)?.Balances?.Current?.ToString("C2") ?? string.Empty
                            )
                        )
                )
                .Concat(
                    response.Liabilities!.Mortgage!
                        .Select(x =>
                            new Row(
                                "Mortgage",
                                AccountFor(x.AccountId)?.Name ?? string.Empty,
                                AccountFor(x.AccountId)?.Balances?.Current?.ToString("C2") ?? string.Empty
                            )
                        )
                )
                .ToArray()
        };

        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(DataTable), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Payment()
    {
        var listrequest = new Going.Plaid.PaymentInitiation.PaymentInitiationPaymentListRequest();
        var listresponse = await _client.PaymentInitiationPaymentListAsync(listrequest);

        if (listresponse.Error is not null)
            return Error(listresponse.Error);

        var paymentid = listresponse.Payments.First().PaymentId;
        var request = new Going.Plaid.PaymentInitiation.PaymentInitiationPaymentGetRequest() { PaymentId = paymentid };
        var response = await _client.PaymentInitiationPaymentGetAsync(request);

        if (response.Error is not null)
            return Error(response.Error);

        var result = new DataTable("Payment ID", "Amount/r", "Status", "Status Update", "Recipient ID")
        {
            Rows = new Row[]
            {
                new Row(
                    paymentid,
                    response.Amount?.Value.ToString("C2") ?? string.Empty,
                    response.Status.ToString(),
                    response.LastStatusUpdate.ToString("MM-dd"),
                    response.RecipientId
                )
            }
        };

        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(DataTable), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Assets()
    {
        _client.AccessToken = null;
        var createrequest = new Going.Plaid.AssetReport.AssetReportCreateRequest()
        {
            AccessTokens = new[] { _credentials.AccessToken! },
            DaysRequested = 10,
            Options = new ()
            {
                ClientReportId = "Custom Report ID #123",
                User = new()
                {
                    ClientUserId = "Custom User ID #456",
                    FirstName = "Alice",
                    MiddleName = "Bobcat",
                    LastName = "Cranberry",
                    Ssn = "123-45-6789",
                    PhoneNumber = "555-123-4567",
                    Email = "alice@example.com"
                }
            }
        };
        var createresponse = await _client.AssetReportCreateAsync(createrequest);

        if (createresponse.Error is not null)
            return Error(createresponse.Error);

        var request = new Going.Plaid.AssetReport.AssetReportGetRequest() 
        { 
            AssetReportToken = createresponse.AssetReportToken            
        };

        var response = await _client.AssetReportGetAsync(request);
        int retries = 10;
        while (response?.Error?.ErrorCode == ErrorCode.ProductNotReady && retries-- > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            response = await _client.AssetReportGetAsync(request);
        }

        if (response?.Error is not null)
            return Error(response.Error);

        var result = new DataTable("Account", "Transactions/r", "Balance/r", "Days Available/r")
        {
            Rows = response!.Report.Items
                .SelectMany(x => x.Accounts.Select( a =>
                    new Row(
                        a.Name,
                        a.Transactions.Count.ToString(),
                        a.Balances.Current?.ToString("C2") ?? string.Empty,
                        a.DaysAvailable.ToString("0")
                    ))
                )
                .ToArray()
        };

        // This would be the time to get the PDF report, however I don't see that Going.Plaid has that
        // ability.
        //
        // https://github.com/viceroypenguin/Going.Plaid/issues/63

        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(DataTable), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Transfer()
    {
        var actrequest = new Going.Plaid.Accounts.AccountsGetRequest();
        var actresponse = await _client.AccountsGetAsync(actrequest);

        if (actresponse.Error is not null)
            return Error(actresponse.Error);

        var accountid = actresponse.Accounts.FirstOrDefault()?.AccountId;
        var transrequest = new Going.Plaid.Transfer.TransferAuthorizationCreateRequest()
        {
            AccountId = accountid!,
            Amount = "1.34",
            Network = TransferNetwork.Ach,
            AchClass = AchClass.Ppd,
            Type = TransferType.Credit,
            User = new()
            {
                LegalName = "Alice Cranberry",
                PhoneNumber = "555-123-4567",
                EmailAddress = "alice@example.com"
            }
        };
        var transresponse = await _client.TransferAuthorizationCreateAsync(transrequest);

        if (transresponse.Error is not null)
            return Error(transresponse.Error);

        _logger.LogInformation($"Transfer Auth OK: {JsonSerializer.Serialize(transresponse)}");

        var authid = transresponse.Authorization.Id;

        var createrequest = new Going.Plaid.Transfer.TransferCreateRequest()
        {
            IdempotencyKey = "1223abc456xyz7890001",
            AccountId = accountid!,
            AuthorizationId = authid,
            Amount = "1.34",
            Network = TransferNetwork.Ach,
            AchClass = AchClass.Ppd,
            Type = TransferType.Credit,
            User = new()
            {
                LegalName = "Alice Cranberry",
                PhoneNumber = "555-123-4567",
                EmailAddress = "alice@example.com"
            }
        };
        var createresponse = await _client.TransferCreateAsync(createrequest);

        if (createresponse.Error is not null)
            return Error(createresponse.Error);

        _logger.LogInformation($"Transfer Create OK: {JsonSerializer.Serialize(createresponse)}");

        var transferid = createresponse.Transfer.Id;

        var request = new Going.Plaid.Transfer.TransferGetRequest()
        {
            TransferId = transferid,
        };
        var response = await _client.TransferGetAsync(request);

        if (response.Error is not null)
            return Error(response.Error);

        var result = new DataTable("Transfer ID", "Amount/r", "Type", "ACH Class", "Network", "Status")
        {
            Rows = new Row[]
            {
                new Row(
                    transferid,
                    response.Transfer.Amount,
                    response.Transfer.Type.ToString(),
                    response.Transfer.AchClass.ToString(),
                    response.Transfer.AchClass.ToString(),
                    response.Transfer.Status.ToString()
                )
            }
        };

        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(DataTable), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Verification()
    {
        var request = new Going.Plaid.Accounts.AccountsBalanceGetRequest();

        var response = await _client.AccountsBalanceGetAsync(request);

        if (response.Error is not null)
            return Error(response.Error);

        var result = new DataTable("Description", "Current Amount/r", "Currency")
        {
            Rows = response.Accounts
                .Select(x =>
                    new Row(
                        x.Name,
                        x.AccountId,
                        x.Balances?.Current?.ToString("C2") ?? string.Empty
                    )
                )
                .ToArray()
        };

        return Ok(result);
    }

    ObjectResult Error(Going.Plaid.Errors.PlaidError error, [CallerMemberName] string callerName = "")
    {
        _logger.LogError($"{callerName}: {JsonSerializer.Serialize(error)}");
        return StatusCode(StatusCodes.Status400BadRequest, error);
    }
}
