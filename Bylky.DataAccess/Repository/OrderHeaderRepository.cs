using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BulkyBook.DataAccess.Repository
{
	public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
	{
        private readonly ApplicationDbContext _db;

        /* That way whatever  DB context we get here we will pass to the repository (base) */
        public OrderHeaderRepository(ApplicationDbContext db) : base(db) 
        {
            _db = db;
        }


        public void Update(OrderHeader obj)
        {
            _db.OrderHeaders.Update(obj);
        }

		public void UpdateStatus(int id, string orderStatus, string? paymentStatus = null)
		{
			var orderFromDb = _db.OrderHeaders.FirstOrDefault(u => u.Id == id);
			if (orderFromDb != null) 
			{
				orderFromDb.OrderStatus = orderStatus;
				if (!String.IsNullOrEmpty(paymentStatus)) 
				{
					orderFromDb.PaymentStatus = paymentStatus;
				}
			}
		}

		public void UpdateStripePaymentId(int id, string sessionId, string paymentIntentId)
		{
			var orderFromDb = _db.OrderHeaders.FirstOrDefault(u => u.Id == id);
			//A sessionId gets generated when a user tries to make apayment
			if (!String.IsNullOrEmpty(sessionId)) 
			{
				orderFromDb.SessionId = sessionId;
			}
			if (!String.IsNullOrEmpty(paymentIntentId)) 
			{
				orderFromDb.PaymentIntendId = paymentIntentId;
				orderFromDb.PaymentDate = DateTime.Now;
			}
		}
	}
}
