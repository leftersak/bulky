﻿using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
	[Area("Admin")]
    [Authorize]
	public class OrderController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderVM  OrderVM { get; set; }   

        public OrderController(IUnitOfWork unitOfWork)
        {
			_unitOfWork = unitOfWork;
			 
		}
        public IActionResult Index()
		{
			return View();
		}

        public IActionResult Details(int orderId)
        {
            OrderVM orderVM = new(){
                OrderHeader = _unitOfWork.OrderHeader.Get(u=>u.Id ==orderId,includeProperties:"ApplicationUser"),
                OrderDetails = _unitOfWork.OrderDetail.GetAll(u=>u.OrderHeaderId == orderId,includeProperties:"Product")
            };
            return View(orderVM);
        }


        [HttpPost]
        [Authorize(Roles = (SD.Role_Admin+","+SD.Role_Employee))]
        public IActionResult UpdateOrderDetail()
        {
            //  OrderVM.OrderHeader.Id we have this in the hidden property in Details.cshtml
            var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u=>u.Id == OrderVM.OrderHeader.Id);

            orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = OrderVM.OrderHeader.City;
            orderHeaderFromDb.State = OrderVM.OrderHeader.State;
            orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;
            if (!String.IsNullOrEmpty(OrderVM.OrderHeader.Carrier))
            {
                orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            }
            if (!String.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber))
            {
                orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            }
            _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
            _unitOfWork.Save();

            TempData["Success"] = "Order Details Updated Succesfully!";

            return RedirectToAction(nameof(Details), new {orderId = orderHeaderFromDb.Id });
        }

        [HttpPost]
        [Authorize(Roles = (SD.Role_Admin + "," + SD.Role_Employee))]
        public IActionResult StartProcessing()
        {
            _unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusInProcess);
            _unitOfWork.Save();

            TempData["Success"] = "Order Details Updated Succesfully!";

            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
        }
        
        
        [HttpPost]
        [Authorize(Roles = (SD.Role_Admin + "," + SD.Role_Employee))]
        public IActionResult ShipOrder()
        {

            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);

            orderHeader.Carrier = OrderVM.OrderHeader.Carrier;
            orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;
            
            if (OrderVM.OrderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
            }

            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.Save();


            TempData["Success"] = "Order Shipped  Succesfully!";

            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
        }


        [HttpPost]
        [Authorize(Roles = (SD.Role_Admin + "," + SD.Role_Employee))]
        public IActionResult CancelOrder()
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);

            if (orderHeader.PaymentStatus == SD.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeader.PaymentIntendId
                };
                var service = new RefundService();
                Refund refundService = service.Create(options);


                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCanceled, SD.StatusRefunded);
            }
            else
            {
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCanceled, SD.StatusCanceled);
            }

            _unitOfWork.Save();
            TempData["Success"] = "Order Canceled  Succesfully!";

            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });

        }


        [ActionName("Details")]
        [HttpPost]
        public IActionResult Details_PAY_NOW()
        {
            //Because when we are posting we might lose some information
            OrderVM.OrderHeader = _unitOfWork.OrderHeader
                .Get(u => u.Id == OrderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
            OrderVM.OrderDetails = _unitOfWork.OrderDetail
                .GetAll(u => u.OrderHeader.Id == OrderVM.OrderHeader.Id, includeProperties: "Product");

            //It is a regular customer account and we need to capture payment
            //Stripe logic
            var domain = "https://localhost:7228/";
            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader.Id}",
                CancelUrl = domain + $"admin/order/details?orderId={OrderVM.OrderHeader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",

            };

            foreach (var item in OrderVM.OrderDetails)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Product.Price * 100),//$20.50 => 2050
                        Currency = "EUR",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title
                        }
                    },

                    Quantity = item.Count
                };
                options.LineItems.Add(sessionLineItem);
            }

            var service = new SessionService();
            Session session = service.Create(options);
            _unitOfWork.OrderHeader.UpdateStripePaymentId(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.Save();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);


            
        }


        public IActionResult PaymentConfirmation(int orderHeaderId)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderHeaderId, includeProperties: "ApplicationUser");
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                //this is an order by a company
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentId(orderHeaderId, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }

                List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart.
                    GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
                _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
                _unitOfWork.Save();
            }


            return View(orderHeaderId);
        }



        #region API CALLS
        [HttpGet]
		public IActionResult GetAll(string status)
		{
            IEnumerable<OrderHeader> objOrderHeaders; 

            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                objOrderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();

            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                //We will have to filter that based on the UserId
                objOrderHeaders = _unitOfWork.OrderHeader.GetAll(u => u.ApplicationUserId == userId, includeProperties: "ApplicationUser").ToList();
            }

            switch (status)
            {
                case "pending":
                    objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;
                default:
                    break;
            }


            return Json(new { data = objOrderHeaders });
		}


		
		#endregion
	}
}