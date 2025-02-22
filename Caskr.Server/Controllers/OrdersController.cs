using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caskr.server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController(IOrdersService ordersService) : ControllerBase
    {
        // GET: api/Orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            return (await ordersService.GetOrdersAsync()).ToList();
        }

        // GET: api/Orders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await ordersService.GetOrderAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            return order;
        }

        // PUT: api/Orders/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<ActionResult<Order>> PutOrder(int id, Order order)
        {
            if (id != order.Id)
            {
                return BadRequest();
            }

            return await ordersService.UpdateOrderAsync(order);
        }

        // POST: api/Orders
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Order>> PostOrder(Order? order)
        {
            var createdOrder = await ordersService.AddOrderAsync(order);
            return CreatedAtAction("GetOrder", new { id = createdOrder.Id }, createdOrder);
        }

        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await ordersService.GetOrderAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            await ordersService.DeleteOrderAsync(id);

            return NoContent();
        }
    }
}
