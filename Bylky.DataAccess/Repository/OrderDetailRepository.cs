using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BulkyBook.DataAccess.Repository
{
    public class OrderDetailRepository : Repository<OrderDetail> , IOrderDetailRepository
    {
        private readonly ApplicationDbContext _db;

        /* That way whatever  DB context we get here we will pass to the repository (base) */
        public OrderDetailRepository(ApplicationDbContext db) : base(db) 
        {
            _db = db;
        }


        public void Update(OrderDetail obj)
        {
            _db.OrderDetails.Update(obj);
        }
    }
}
