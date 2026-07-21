using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TheBugTracker.Client.Models;
using TheBugTracker.Client.Models.Enums;
using TheBugTracker.Client.Services.Interfaces;
using TheBugTracker.Client.UserInfoHelper;

namespace TheBugTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectsController(IProjectDTOService projectService) : ControllerBase
    {
        UserInfo UserInfo => UserInfoHelper.GetUserInfo(User)!;

        /// <summary>
        /// Get Projects
        /// </summary>
        /// <remarks>
        /// Get all active projects belonging to the user's company.
        /// </remarks>
        /// <param name="filter">
        /// An optional filter applied to the projects that will be returned.
        /// Defaults to **active** projects.
        /// </param>
        [HttpGet] 
        public async Task<ActionResult<IEnumerable<ProjectDTO>>> GetProjects([FromQuery]ProjectsFilter filter = ProjectsFilter.Active)
        {
            IEnumerable<ProjectDTO> projects = filter switch
            {
                ProjectsFilter.Archived => await projectService.GetArchivedProjectAsync(UserInfo),
                ProjectsFilter.Assigned => await projectService.GetAssignedProjectAsync(UserInfo),
                _ => await projectService.GetProjectAsync(UserInfo)
            };

            return Ok(projects);
        }

        /// <summary>
        /// Get Project By ID
        /// </summary>
        /// <remarks>
        /// Get detailed information about a *specific* project,
        /// if it exists and the user is authorized to view it.
        /// </remarks>
        /// <param name="projectId">The ID of the project to retrieve</param>
        [HttpGet("{projectId:int}")]
        public async Task<ActionResult<ProjectDTO>> GetProjectById([FromRoute] int projectId)
        {
            ProjectDTO? project = await projectService.GetProjectByIdAsync(projectId, UserInfo);

            if(project is null)
            {
                return NotFound();
            }

            return Ok(project);
        }

        /// <summary>
        /// Create Project
        /// </summary>
        /// <remarks>
        /// Creates a new project for the user's company
        ///
        /// Users must be a project manager or admin to submit a new
        /// project. If the user is a project manager, they will be
        /// assigned to the submitted project.
        /// </remarks>
        /// <param name="project">The details of the project to be created</param>
        [HttpPost]
        [Authorize(Roles = $"{nameof(Role.Admin)}, {nameof(Role.ProjectManager)}")]
        public async Task<ActionResult<ProjectDTO>> CreateProject([FromBody] ProjectDTO project)
        {
            ProjectDTO createdProject = await projectService.CreateProjectAsync(project, UserInfo);

            return CreatedAtAction(
                actionName: nameof(GetProjectById),
                routeValues: new { projectId = createdProject.Id },
                value: createdProject
            );
        }

        /// <summary>
        /// Update Project
        /// </summary>
        /// <remarks>
        /// Update the details of a specific project, if it exists.
        ///
        /// Users must be an admin or the project manager assigned to the
        /// project to submit an update.
        /// </remarks>
        /// <param name="projectId">The ID of the project to update</param>
        /// <param name="project">The updated details for this project</param>
        [HttpPut("{projectId:int}")]
        [Authorize(Roles = $"{nameof(Role.Admin)}, {nameof(Role.ProjectManager)}")]
        public async Task<IActionResult> UpdateProject([FromRoute] int projectId, [FromBody] ProjectDTO project)
        {
            if(projectId != project.Id)
            {
                return BadRequest();
            }

            await projectService.UpdateProjectAsync(project, UserInfo);

            return NoContent();
        }

        /// <summary>
        /// Archive Project
        /// </summary>
        /// <remarks>
        /// Archive a project to indicate it is no longer being worked on.
        ///
        /// Only admins or the project manager assigned to the project
        /// may archive a project.
        /// </remarks>
        /// <param name="projectId">The ID of the project to archive</param>
        [HttpPatch("{projectId:int}/archive")]
        [Authorize(Roles = $"{nameof(Role.Admin)}, {nameof(Role.ProjectManager)}")]
        public async Task<IActionResult> ArchiveProject([FromRoute] int projectId)
        {
            await projectService.ArchiveProjectAsync(projectId, UserInfo);

            return NoContent();
        }

        /// <summary>
        /// Restore Project
        /// </summary>
        /// <remarks>
        /// Restore a project to indicate it is active and work will resume.
        ///
        /// Only admins or the project manager assigned to the project
        /// may restore a project.
        /// </remarks>
        /// <param name="projectId">The ID of the project to archive</param>
        [HttpPatch("{projectId:int}/restore")]
        [Authorize(Roles = $"{nameof(Role.Admin)}, {nameof(Role.ProjectManager)}")]
        public async Task<IActionResult> RestoreProject([FromRoute] int projectId)
        {
            await projectService.RestoreProjectAsync(projectId, UserInfo);

            return NoContent();
        }

        /// <summary>
        /// Add Project Member
        /// </summary>
        /// <param name="projectId">The ID of the project</param>
        /// <param name="userId">The ID of the user</param>
        /// <remarks>
        /// Assigns a user to a specific project, if they are not
        /// already assigned
        /// 
        /// *Note: Project Managers must be assigned by the Assign Project Manager
        /// endpoint. Admins cannot be assigned to projects.*
        /// 
        /// *Only Admins and the assigned Project Manager may add
        /// members to a project.*
        /// </remarks>
        [HttpPut("{projectId:int}/members/{userId}")]
        [Authorize(Roles = $"{nameof(Role.Admin)}, {nameof(Role.ProjectManager)}")]
        public async Task<IActionResult> AddMemberToProject([FromRoute] int projectId, [FromRoute] string userId)
        {
            await projectService.AddProjectMemberAsync(projectId, userId, UserInfo);
            return NoContent();
        }

        /// <summary>
        /// Get Project Members
        /// </summary>
        /// <param name="projectId">The ID of the project</param>
        /// <remarks>
        /// Returns all members assigned to the given project
        /// </remarks>
        [HttpGet("{projectId:int}/members")]
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetProjectMemebers([FromRoute]int projectId)
        {
            IEnumerable<UserDTO> members = await projectService.GetProjectMembersAsync(projectId, UserInfo);

            return Ok(members);
        }

        /// <summary>
        /// Remove Project Member
        /// </summary>
        /// <param name="projectId">The ID of the project</param>
        /// <param name="userId">The ID of the user</param>
        /// <remarks>
        /// Removes a member from a project, if they are assigned to it.
        /// 
        /// *Note: only Admins and the assigned Project Manager may remove
        /// members from a project. The Project Manager must be removed using
        /// the Remove Project Manager endpoint.*
        /// </remarks>
        [HttpDelete("{projectId:int}/members/{userId}")]
        [Authorize(Roles = $"{nameof(Role.Admin)}, {nameof(Role.ProjectManager)}")]
        public async Task<IActionResult> RemoveProjectMember([FromRoute] int projectId, [FromRoute] string userId)
        {
            await projectService.RemoveProjectMemberAsync(projectId, userId, UserInfo);
            return NoContent();
        }

        /// <summary>
        /// Get Project Manager
        /// </summary>
        /// <param name="projectId">The ID of the project</param>
        /// <remarks>
        /// Returns the project manager assigned to the project, if one exists.
        /// If no project manager is assigned or the project does not exist, returns
        /// 404 Not Found.
        /// </remarks>
        [HttpGet("{projectId:int}/manager")]
        public async Task<ActionResult<UserDTO>> GetProjectManager([FromRoute] int projectId)
        {
            UserDTO? projectManager = await projectService.GetProjectManagerAsync(projectId, UserInfo);

            if (projectManager is null)
            {
                return NotFound();
            }

            return Ok(projectManager);
        }

        /// <summary>
        /// Assign Project Manager
        /// </summary>
        /// <param name="projectId">The ID of the project</param>
        /// <param name="userId">The ID of the project manager to assign</param>
        /// <remarks>
        /// Assigns a project manager to a project. If another project manager is currently assigned,
        /// they will be removed and replaced with the new project manager.
        /// 
        /// If the user is not a project manager, they may not be assigned to manage
        /// a project.
        /// </remarks>
        [HttpPut("{projectId:int}/manager/{userId}")]
        [Authorize(Roles = $"{nameof(Role.Admin)}, {nameof(Role.ProjectManager)}")]
        public async Task<IActionResult> AssignProjectManager([FromRoute] int projectId, [FromRoute] string userId)
        {
            await projectService.AssignProjectManagerAsync(projectId, userId, UserInfo);
            return NoContent();
        }

        /// <summary>
        /// Remove Project Manager
        /// </summary>
        /// <param name="projectId">The ID of the project</param>
        /// <remarks>
        /// Unassigns a project's manager, if a project manager is currently assigned.
        /// </remarks>
        [HttpDelete("{projectId:int}/manager")]
        [Authorize(Roles = $"{nameof(Role.Admin)}, {nameof(Role.ProjectManager)}")]
        public async Task<IActionResult> RemoveProjectManager([FromRoute] int projectId)
        {
            await projectService.RemoveProjectManagerAsync(projectId, UserInfo);
            return NoContent();
        }
    }
}
