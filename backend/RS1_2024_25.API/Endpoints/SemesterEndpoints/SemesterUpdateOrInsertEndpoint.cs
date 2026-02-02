using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RS1_2024_25.API.Data;
using RS1_2024_25.API.Data.Enums;
using RS1_2024_25.API.Data.Models.SharedTables;
using RS1_2024_25.API.Data.Models.TenantSpecificTables.Modul1_Auth;
using RS1_2024_25.API.Data.Models.TenantSpecificTables.Modul2_Basic;
using RS1_2024_25.API.Helper.Api;
using RS1_2024_25.API.Services;
using System.ComponentModel.DataAnnotations.Schema;
using static RS1_2024_25.API.Endpoints.SemesterEndpoints.SemesterUpdateOrInsertEndpoint;

namespace RS1_2024_25.API.Endpoints.SemesterEndpoints;

[Route("students")]

[MyAuthorization(isAdmin: true, isManager: false)]
public class SemesterUpdateOrInsertEndpoint(ApplicationDbContext db) : MyEndpointBaseAsync
    .WithRequest<SemesterUpdateOrInsertRequest>
    .WithActionResult<int>
{
    [HttpPost("{studentId}/semesters")]
    public override async Task<ActionResult<int>> HandleAsync(
        [FromBody] SemesterUpdateOrInsertRequest request,
        CancellationToken cancellationToken = default)
    {
        // Check if it's an insert or update operation
        bool isInsert = request.ID == null || request.ID == 0;
        Semester? semester;

        if (isInsert)
        {
            semester = new Semester
            {
            };
            db.Add(semester);
        }
        else
        {
            // Update operation: retrieve the existing semester
            semester = await db.Semesters
                .SingleOrDefaultAsync(x => x.ID == request.ID, cancellationToken);

            if (semester == null)
            {
                return NotFound("Semester not found");
            }
        }

        // Set common properties for both insert and update
      
        semester.AcademicYearId = request.AcademicYearId;
        semester.StudyYear = request.StudyYear;
        semester.EnrollmentDate = request.EnrollmentDate;

        var authInfo = MyAuthServiceHelper
     .GetAuthInfoFromRequest(db, HttpContext.RequestServices.GetService<IHttpContextAccessor>()!);
        var studentId = int.Parse(
       HttpContext.Request.RouteValues["studentId"]!.ToString()!);

        semester.RecordedById = authInfo.UserId;
        semester.TenantId = authInfo.TenantId;
        semester.StudentId = studentId;

        var count = await db.Semesters
      .CountAsync(x => x.StudentId == studentId &&
                       x.StudyYear == semester.StudyYear,
                  cancellationToken);

        if (count == 1)
        {
            semester.IsRenewal = true;
            semester.TuitionFee = 400;
        }
        else if (count >= 2)
        {
            semester.IsRenewal = true;
            semester.TuitionFee = 500;
        }
        else
        {
            semester.IsRenewal = false;
            semester.TuitionFee = 1800;
        }

        if(db.Semesters.ToList().Exists(x=>x.StudentId == studentId && x.AcademicYearId == request.AcademicYearId && !x.IsDeleted))
        {
            throw new Exception("Akademska godina za studenta vec postoji!");
        }

        // Save changes to the database
        await db.SaveChangesAsync(cancellationToken);

        return Ok(semester.ID); // Return the ID of the semester
    }

    public class SemesterUpdateOrInsertRequest
    {
        public int? ID { get; set; } // Nullable to allow null for insert operations
        public int AcademicYearId { get; set; }
        public int StudyYear { get; set; }
        public DateTime EnrollmentDate { get; set; }
    }
}
