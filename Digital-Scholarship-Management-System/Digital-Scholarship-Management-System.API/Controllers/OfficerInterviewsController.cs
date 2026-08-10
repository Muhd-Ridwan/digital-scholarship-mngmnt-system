using Microsoft.AspNetCore.Mvc;

namespace Digital_Scholarship_Management_System.API.Controllers
{
    public class InterviewsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
