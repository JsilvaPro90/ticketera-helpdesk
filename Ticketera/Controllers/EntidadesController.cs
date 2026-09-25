using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Ticketera.Models;

namespace Ticketera.Controllers
{
    [Authorize]
    public class EntidadesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Entidades
        public ActionResult Index()
        {
            var entidades = db.Entidades
                .Include(e => e.Contactos)
                .OrderBy(e => e.Nombre)
                .ToList();

            return View(entidades);
        }

        // GET: Entidades/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Entidad entidad = db.Entidades
                .Include(e => e.Contactos)
                .Include(e => e.Tickets)
                .FirstOrDefault(e => e.Id == id);

            if (entidad == null)
            {
                return HttpNotFound();
            }

            return View(entidad);
        }

        // GET: Entidades/Create
        public ActionResult Create()
        {
            Entidad entidad = new Entidad
            {
                Activo = true,
                FechaRegistro = DateTime.Now
            };

            return View(entidad);
        }

        // POST: Entidades/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Entidad entidad)
        {
            if (ModelState.IsValid)
            {
                entidad.FechaRegistro = DateTime.Now;

                db.Entidades.Add(entidad);
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(entidad);
        }

        // GET: Entidades/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Entidad entidad = db.Entidades.Find(id);

            if (entidad == null)
            {
                return HttpNotFound();
            }

            return View(entidad);
        }

        // POST: Entidades/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Entidad model)
        {
            ModelState.Remove("Contactos");
            ModelState.Remove("Tickets");

            if (ModelState.IsValid)
            {
                Entidad entidadDb = db.Entidades.Find(model.Id);

                if (entidadDb == null)
                {
                    return HttpNotFound();
                }

                entidadDb.Nombre = model.Nombre;
                entidadDb.Ruc = model.Ruc;
                entidadDb.Direccion = model.Direccion;
                entidadDb.Email = model.Email;
                entidadDb.Telefono = model.Telefono;
                entidadDb.Activo = model.Activo;

                db.SaveChanges();

                return RedirectToAction("Details", new { id = entidadDb.Id });
            }

            return View(model);
        }

        // GET: Entidades/Delete/5
        // GET: Entidades/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Entidad entidad = db.Entidades
                .Include(e => e.Contactos)
                .Include(e => e.Tickets)
                .FirstOrDefault(e => e.Id == id);

            if (entidad == null)
            {
                return HttpNotFound();
            }

            return View(entidad);
        }

        // POST: Entidades/Delete/5
        // POST: Entidades/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Entidad entidad = db.Entidades
                .Include(e => e.Contactos)
                .Include(e => e.Tickets)
                .FirstOrDefault(e => e.Id == id);

            if (entidad == null)
            {
                return HttpNotFound();
            }

            bool tieneContactos = entidad.Contactos != null && entidad.Contactos.Any();
            bool tieneTickets = entidad.Tickets != null && entidad.Tickets.Any();

            if (tieneContactos || tieneTickets)
            {
                TempData["Error"] = "No se puede eliminar la empresa porque tiene contactos o tickets registrados. Puedes marcarla como inactiva.";
                return RedirectToAction("Index");
            }

            db.Entidades.Remove(entidad);
            db.SaveChanges();

            TempData["Mensaje"] = "Empresa eliminada correctamente.";

            return RedirectToAction("Index");
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