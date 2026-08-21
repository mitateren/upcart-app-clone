using Microsoft.AspNetCore.Mvc;

namespace UpCard.Web.Pages;

public class HelpModel : UpCardPageModel
{
    public IActionResult OnGet() => Page();
}
