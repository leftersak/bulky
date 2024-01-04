using BulkyBook.DataAccess.Repository;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;

        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            //How to populate the navigation property using Include
            List<Company> companyList = _unitOfWork.Company.GetAll().ToList();
            
            return View(companyList);
        }

        public IActionResult Upsert(int? id)//Up = Update and sert = Insert
        {
            
            if (id == null || id==0)
            {
                //create
                return View(new Company());
            }
            else
            {
                //update
                Company company = _unitOfWork.Company.Get(u=>u.Id == id);
                return View(company);
            }

        }

        [HttpPost]
        public IActionResult Upsert(Company company)
        {
            
            if (ModelState.IsValid)//For the DataAnnotations
            {
                
                if (company.Id==0)
                {
                    _unitOfWork.Company.Add(company);
                }
                else
                {
                    _unitOfWork.Company.Update(company);
                }

				
                _unitOfWork.Save();
                TempData["success"] = "Company created successfully!";
                return RedirectToAction("Index");
            }
            

            return View(company);

        }


        

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Company> companyList = _unitOfWork.Company.GetAll().ToList();
            return Json(new { data = companyList });
        }


        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var comapnyToBeDeleted = _unitOfWork.Company.Get(u => u.Id == id);
            if (comapnyToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting " });
            }

            

            _unitOfWork.Company.Remove(comapnyToBeDeleted);
            _unitOfWork.Save();


            return Json(new { success = true, message = "Deleted Successful " });
        }
        #endregion

    }
}
