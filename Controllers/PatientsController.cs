﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Ris2022.Data;
using Ris2022.Data.Models;
using Ris2022.Resources;
using System.ComponentModel.DataAnnotations;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using AspNetCoreHero.ToastNotification.Abstractions;
using NToastNotify;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.ReportingServices.ReportProcessing.OnDemandReportObjectModel;

namespace Ris2022.Controllers
{
    [Authorize]
    public class PatientsController : Controller
    {
        private readonly RisDBContext _context;
        private readonly IToastNotification _toastNotification;
        
        
        public PatientsController(RisDBContext context, IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
            
        }


        // GET: Patients
        [Authorize(Policy = "Index+DetailsPatientsPolicy")]
        public async Task<IActionResult> Index()
        {
            var risDBContext = _context.Patients.Include(p => p.Acceptancetype).Include(p => p.Martialstatus).Include(p => p.Nationality).Include(p => p.Worktype).Include(p => p.Reason);
            return View(await risDBContext.ToListAsync());
        }

        // GET: Patients/Details/5
        [Authorize(Policy = "Index+DetailsPatientsPolicy")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Patients == null)
            {
                return NotFound();
            }

            var patients = await _context.Patients.Where(x => x.Id == id)
                .Include(p => p.Acceptancetype)
                .Include(p => p.Martialstatus)
                .Include(p => p.Nationality)
                .Include(p => p.Worktype)
                .Include(p => p.Reason)
                .Take(1).ToListAsync();
            var patient = patients.FirstOrDefault();
            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }

        // GET: Patients/Create
        [Authorize(Policy = "CreatePatientsPolicy")]
        public IActionResult Create()
        {
            ViewData["Acceptancetypeid"] = new SelectList(_context.Acceptancetypes, "Id", Resource.ENARName);
            ViewData["Martialstatusid"] = new SelectList(_context.Martialstatuses, "Id", Resource.ENARName);
            ViewData["Nationalityid"] = new SelectList(_context.Nationalities, "Id", Resource.ENARName);
            ViewData["Worktypeid"] = new SelectList(_context.Worktypes, "Id", Resource.ENARName);
            ViewData["Reasonid"] = new SelectList(_context.Reasons, "Id", Resource.ENARName);
            ViewData["Gendre"] = new SelectList(Enum.GetValues(typeof(gender)));
            Patient patient = new()
            {
                
                InsertUserName = User.FindFirstValue(ClaimTypes.Name),
                Givenid = "pat" + DateTime.Now.ToString("ddMMyyyyHHmmss")
            };
            return View(patient);
            
            
        }

        // post: Patients/Convert Arabic Text toEnglish

        [HttpPost]
        public string toEnglish(string araString)
        {
            string result = "";
            StringBuilder sb = new StringBuilder(araString);
            #region Mapping letters
            sb.Replace("ا", "a");
            sb.Replace(" ", " ");

            sb.Replace("ء", "a");
            sb.Replace("ؤ", "ou");
            sb.Replace("ئ", "e");
            sb.Replace("أ", "a");
            sb.Replace("ة", "a");
            sb.Replace("إ", "e");
            sb.Replace("ى", "a");
            sb.Replace("ب", "b");
            sb.Replace("ت", "t");
            sb.Replace("ث", "th");
            sb.Replace("ج", "g");
            sb.Replace("ح", "h");
            sb.Replace("خ", "kh");
            sb.Replace("د", "d");
            sb.Replace("ذ", "th");
            sb.Replace("ر", "r");
            sb.Replace("ز", "z");
            sb.Replace("س", "s");
            sb.Replace("ش", "sh");
            sb.Replace("ص", "s");
            sb.Replace("ض", "d");
            sb.Replace("ط", "t");
            sb.Replace("ظ", "z");
            sb.Replace("ع", "a");
            sb.Replace("غ", "g");
            sb.Replace("ف", "f");
            sb.Replace("ق", "k");
            sb.Replace("ك", "k");
            sb.Replace("ل", "l");
            sb.Replace("م", "m");
            sb.Replace("ن", "n");
            sb.Replace("ه", "h");
            sb.Replace("و", "w");
            sb.Replace("ي", "y");
            #endregion
            result = sb.ToString();
            return result;
        }

        // POST: Patients/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CreatePatientsPolicy")]
        public async Task<IActionResult> Create(Patient patient)
        {
            patient.Translatedfname = toEnglish(patient.Firstname);
            patient.Translatedlname = toEnglish(patient.Lastname);

            if (ModelState.IsValid)
            {
                _context.Add(patient);
                await _context.SaveChangesAsync();
                _toastNotification.AddSuccessToastMessage("تم الحفظ بنجاح");
                return RedirectToAction(nameof(Index));
                
            }
            ViewData["Acceptancetypeid"] = new SelectList(_context.Acceptancetypes, "Id", Resource.ENARName, patient.Acceptancetypeid);
            ViewData["Martialstatusid"] = new SelectList(_context.Martialstatuses, "Id", Resource.ENARName, patient.Martialstatusid);
            ViewData["Nationalityid"] = new SelectList(_context.Nationalities, "Id", Resource.ENARName, patient.Nationalityid);
            ViewData["Worktypeid"] = new SelectList(_context.Worktypes, "Id", Resource.ENARName, patient.Worktypeid);
            ViewData["Reasonid"] = new SelectList(_context.Reasons, "Id", Resource.ENARName, patient.Reasonid);
            ViewData["Gendre"] = new SelectList(Enum.GetValues(typeof(gender)),patient.Gendre);
            ViewData["Gendre"] = new SelectList(Enum.GetValues(typeof(gender)), patient.Gendre);
            return View(patient);
         

        }

        // GET: Patients/Edit/5
        [Authorize(Policy = "EditPatientsPolicy")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Patients == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return NotFound();
            }
            ViewData["Acceptancetypeid"] = new SelectList(_context.Acceptancetypes, "Id", "Id", patient.Acceptancetypeid);
            ViewData["Martialstatusid"] = new SelectList(_context.Martialstatuses, "Id", "Id", patient.Martialstatusid);
            ViewData["Nationalityid"] = new SelectList(_context.Nationalities, "Id", "Id", patient.Nationalityid);
            ViewData["Worktypeid"] = new SelectList(_context.Worktypes, "Id", "Id", patient.Worktypeid);
            ViewData["Reasonid"] = new SelectList(_context.Reasons, "Id", Resource.ENARName, patient.Reasonid);
            ViewData["Gendre"] = new SelectList(Enum.GetValues(typeof(gender)), patient.Gendre);

            return View(patient);
        }

        // POST: Patients/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "EditPatientsPolicy")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Givenid,Firstname,Middlename,Lastname,Gendre,Mothername,Birthdate,Age,Mobilephone,Landphone,Currentaddress,Residentaddress,Workphone,Workaddress,Nearestperson,Nearestpersonphone,Birthplace,Notes,Translatedfname,Translatedlname,Translatedfathername,Translatedmothername,Insertdate,UpdateuserName,Updatedate,Reasonid,InsertUserName,Nationalityid,Worktypeid,Martialstatusid,Acceptancetypeid")] Patient patient)
        {
            if (id != patient.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    patient.Translatedfname = toEnglish(patient.Firstname);
                    patient.Translatedlname = toEnglish(patient.Lastname);
                    _context.Update(patient);
                    await _context.SaveChangesAsync();
                    _toastNotification.AddSuccessToastMessage("تم التعديل بنجاح");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(patient.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                        _toastNotification.AddErrorToastMessage("تعديل فاشل");
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["Acceptancetypeid"] = new SelectList(_context.Acceptancetypes, "Id", "Id", patient.Acceptancetypeid);
            ViewData["Martialstatusid"] = new SelectList(_context.Martialstatuses, "Id", "Id", patient.Martialstatusid);
            ViewData["Nationalityid"] = new SelectList(_context.Nationalities, "Id", "Id", patient.Nationalityid);
            ViewData["Worktypeid"] = new SelectList(_context.Worktypes, "Id", "Id", patient.Worktypeid);
            ViewData["Reasonid"] = new SelectList(_context.Reasons, "Id", Resource.ENARName, patient.Reasonid);

            return View(patient);
        }


        // GET: Patients/Delete/5
        [Authorize(Policy = "DeletePatientsPolicy")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Patients == null)
            {
                return NotFound();
            }

            var patients = await _context.Patients.Where(x => x.Id == id)
                .Include(p => p.Acceptancetype)
                .Include(p => p.Martialstatus)
                .Include(p => p.Nationality)
                .Include(p => p.Worktype)
                .Include(p => p.Reason)
                .Take(1).ToListAsync();
            var patient = patients.FirstOrDefault();
            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }

        // POST: Patients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "DeletePatientsPolicy")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Patients == null)
            {
                return Problem("Entity set 'RisDBContext.Patients'  is null.");
            }
            var patient = await _context.Patients.FindAsync(id);
            if (patient != null)
            {
                _context.Patients.Remove(patient);
                _toastNotification.AddSuccessToastMessage("تم الحذف بنجاح");
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PatientExists(int id)
        {
            return (_context.Patients?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
