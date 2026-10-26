using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using MyStore.Models;
using MyStore.Models.ViewModel;
using PagedList;

namespace MyStore.Areas.Admin.Controllers
{
    public class ProductsController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();

        // GET: Admin/Products
        public ActionResult Index(string searchTerm, decimal? minPrice, decimal? maxPrice, string sortOrder, int? page)
        {
            var model = new ProductSearchVM();
            var products = db.Products.Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                products = products.Where(p => p.ProductName.Contains(searchTerm) ||
                                               p.ProductDescription.Contains(searchTerm) ||
                                               p.Category.CategoryName.Contains(searchTerm));
            }

            if (minPrice.HasValue)
            {
                products = products.Where(p => p.ProductPrice >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.ProductPrice <= maxPrice.Value);
            }

            switch (sortOrder)
            {
                case "name_asc":
                    products = products.OrderBy(p => p.ProductName);
                    break;
                case "name_desc":
                    products = products.OrderByDescending(p => p.ProductName);
                    break;
                case "price_asc":
                    products = products.OrderBy(p => p.ProductPrice);
                    break;
                case "price_desc":
                    products = products.OrderByDescending(p => p.ProductPrice);
                    break;
                default:
                    products = products.OrderBy(p => p.ProductName);
                    break;
            }

            int pageSize = 5;
            int pageNumber = (page ?? 1);

            model.SearchTerm = searchTerm;
            model.MinPrice = minPrice;
            model.MaxPrice = maxPrice;
            model.SortOrder = sortOrder;
            model.Products = products.ToPagedList(pageNumber, pageSize);

            return View(model);
        }

        // GET: Admin/Products/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Include(p => p.Category).FirstOrDefault(p => p.ProductID == id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // GET: Admin/Products/Create
        public ActionResult Create()
        {
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName");
            return View();
        }

        // POST: Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ProductID,CategoryID,ProductName,ProductDescription,ProductPrice,ProductImage,UploadImg")] Product product)
        {
            if (ModelState.IsValid)
            {
                if (product.UploadImg != null)
                {
                    SaveUploadedImage(product);
                }
                else
                {
                    product.ProductImage = "~/Content/images/default_img.png";
                }

                db.Products.Add(product);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", product.CategoryID);
            return View(product);
        }

        // GET: Admin/Products/Edit/5  (tái sử dụng Details)
        public ActionResult Edit(int? id)
        {
            var result = Details(id);
            var viewResult = result as ViewResult;
            if (viewResult != null)
            {
                var product = (Product)viewResult.Model;
                ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", product.CategoryID);
            }
            return result;
        }

        // POST: Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ProductID,CategoryID,ProductName,ProductDescription,ProductPrice,ProductImage,UploadImg")] Product product)
        {
            if (ModelState.IsValid)
            {
                // Chỉ ghi đè ảnh khi người dùng chọn ảnh mới; ngược lại giữ ProductImage cũ (hidden field)
                if (product.UploadImg != null)
                {
                    SaveUploadedImage(product);
                }

                db.Entry(product).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", product.CategoryID);
            return View(product);
        }

        // GET: Admin/Products/Delete/5  (tái sử dụng Details)
        public ActionResult Delete(int? id)
        {
            return Details(id);
        }

        // POST: Admin/Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }

            try
            {
                db.Products.Remove(product);
                db.SaveChanges();
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException)
            {
                // Sản phẩm đã nằm trong OrderDetail nên không xóa được (ràng buộc khóa ngoại)
                ModelState.AddModelError("", "Không thể xóa sản phẩm này vì đã tồn tại trong đơn hàng.");
                product = db.Products.Include(p => p.Category).FirstOrDefault(p => p.ProductID == id);
                return View(product);
            }
            return RedirectToAction("Index");
        }

        // Hàm dùng chung cho Create và Edit: lưu file ảnh upload
        private void SaveUploadedImage(Product product)
        {
            string filename = Path.GetFileName(product.UploadImg.FileName);
            string savePath = "~/Content/images/";
            product.ProductImage = savePath + filename;
            product.UploadImg.SaveAs(Path.Combine(Server.MapPath(savePath), filename));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
