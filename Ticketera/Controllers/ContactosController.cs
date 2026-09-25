using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Ticketera.Models;

namespace Ticketera.Controllers
{
    [Authorize]
    public class ContactosController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Contactos
        public ActionResult Index()
        {
            var contactos = db.Contactos
                .Include(c => c.Entidad)
                .OrderBy(c => c.Entidad.Nombre)
                .ThenBy(c => c.Nombre)
                .ToList();

            return View(contactos);
        }

        // GET: Contactos/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Contacto contacto = db.Contactos
                .Include(c => c.Entidad)
                .FirstOrDefault(c => c.Id == id);

            if (contacto == null)
            {
                return HttpNotFound();
            }

            return View(contacto);
        }

        // GET: Contactos/Create
        public ActionResult Create(int? entidadId)
        {
            Contacto contacto = new Contacto
            {
                Activo = true,
                EsPrincipal = false,
                FechaRegistro = DateTime.Now
            };

            if (entidadId.HasValue)
            {
                contacto.EntidadId = entidadId.Value;
            }

            CargarEmpresas(contacto.EntidadId);

            return View(contacto);
        }

        // POST: Contactos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Contacto contacto)
        {
            // FechaRegistro no debe validarse desde el formulario
            ModelState.Remove("FechaRegistro");
            ModelState.Remove("Entidad");

            if (ModelState.IsValid)
            {
                contacto.FechaRegistro = DateTime.Now;

                db.Contactos.Add(contacto);
                db.SaveChanges();

                if (contacto.EsPrincipal)
                {
                    MarcarComoPrincipal(contacto.Id, contacto.EntidadId);
                }

                return RedirectToAction("Details", "Entidades", new { id = contacto.EntidadId });
            }

            CargarEmpresas(contacto.EntidadId);

            return View(contacto);
        }

        // GET: Contactos/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Contacto contacto = db.Contactos
                .Include(c => c.Entidad)
                .FirstOrDefault(c => c.Id == id);

            if (contacto == null)
            {
                return HttpNotFound();
            }

            CargarEmpresas(contacto.EntidadId);

            return View(contacto);
        }

        // POST: Contactos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Contacto contacto)
        {
            // Evita error de validación por FechaRegistro
            ModelState.Remove("FechaRegistro");
            ModelState.Remove("Entidad");

            if (ModelState.IsValid)
            {
                Contacto contactoDb = db.Contactos.Find(contacto.Id);

                if (contactoDb == null)
                {
                    return HttpNotFound();
                }

                contactoDb.EntidadId = contacto.EntidadId;
                contactoDb.Nombre = contacto.Nombre;
                contactoDb.Cargo = contacto.Cargo;
                contactoDb.Email = contacto.Email;
                contactoDb.Telefono = contacto.Telefono;
                contactoDb.EsPrincipal = contacto.EsPrincipal;
                contactoDb.Activo = contacto.Activo;

                // No se modifica FechaRegistro
                db.SaveChanges();

                if (contactoDb.EsPrincipal)
                {
                    MarcarComoPrincipal(contactoDb.Id, contactoDb.EntidadId);
                }

                return RedirectToAction("Details", "Entidades", new { id = contactoDb.EntidadId });
            }

            CargarEmpresas(contacto.EntidadId);

            return View(contacto);
        }

        // GET: Contactos/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Contacto contacto = db.Contactos
                .Include(c => c.Entidad)
                .FirstOrDefault(c => c.Id == id);

            if (contacto == null)
            {
                return HttpNotFound();
            }

            return View(contacto);
        }

        // POST: Contactos/Delete/5
        // POST: Contactos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Contacto contacto = db.Contactos.Find(id);

            if (contacto == null)
            {
                return HttpNotFound();
            }

            int entidadId = contacto.EntidadId;

            db.Contactos.Remove(contacto);
            db.SaveChanges();

            TempData["Mensaje"] = "Contacto eliminado correctamente.";

            return RedirectToAction("Index");
        }

        private void CargarEmpresas(int? entidadId = null)
        {
            ViewBag.EntidadId = new SelectList(
                db.Entidades
                    .Where(e => e.Activo || e.Id == entidadId)
                    .OrderBy(e => e.Nombre)
                    .ToList(),
                "Id",
                "Nombre",
                entidadId
            );
        }

        private void MarcarComoPrincipal(int contactoId, int entidadId)
        {
            var otrosContactos = db.Contactos
                .Where(c => c.EntidadId == entidadId && c.Id != contactoId)
                .ToList();

            foreach (var contacto in otrosContactos)
            {
                contacto.EsPrincipal = false;
            }

            db.SaveChanges();
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