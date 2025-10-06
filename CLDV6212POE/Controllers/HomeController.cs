using Microsoft.AspNetCore.Mvc;

namespace CLDV6212POE.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewBag.Message = "This is the About page for CLDV6212POE.";
            return View();
        }

        public IActionResult Contact()
        {
            ViewBag.Message = "Contact us for more information.";
            return View();
        }

        public IActionResult Services()
        {
            ViewBag.Message = "Our services include web development and consulting.";
            return View();
        }

        public IActionResult Portfolio()
        {
            ViewBag.Message = "Check out our latest projects and work.";
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SubmitForm(string name, string email, string message)
        {
            ViewBag.Success = $"Thank you {name}! Your message has been received.";
            ViewBag.Name = name;
            ViewBag.Email = email;
            ViewBag.Message = message;
            return View("Contact");
        }

        public IActionResult Download()
        {
            // Simulate file download
            ViewBag.Message = "Download initiated successfully!";
            return View("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}