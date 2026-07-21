
using TheBugTracker.Client.Models;
using TheBugTracker.Client.UserInfoHelper;
using TheBugTracker.Models;

namespace TheBugTracker.Services.Interfaces
{
    public interface IProjectRepository
    {
        /// <summary>
        /// Retrieves all active projects in database for user's company
        /// </summary>
        /// <param name="user">The current user's claims</param>
        public Task<IEnumerable<Project>> GetProjectAsync(UserInfo user);

        /// <summary>
        /// Retrieves all archived projects in database for user's company
        /// </summary>
        /// <param name="user">The current user's claims</param>
        /// <returns> An enumerable of porjects </returns>
        public Task<IEnumerable<Project>> GetArchivedProjectAsync(UserInfo user);

        /// <summary>
        /// Retrieves projects assigned to the current's user
        /// </summary>
        /// <param name="user">The current user's claims</param>
        /// <returns> An enumerable of porjects </returns>
        public Task<IEnumerable<Project>> GetAssignedProjectAsync(UserInfo user);

        /// <summary>
        /// Retrieve detailed information about a specific project by ID,
        /// if it exists in the user's company
        /// </summary>
        /// <param name="id">The ID of the project to retrieve</param>
        /// <param name="user">The current user's claims</param>
        /// <returns>The requested project, or null if it does not exist</returns>
        public Task<Project?> GetProjectByIdAsync(int id, UserInfo user);

        /// <summary>
        /// Create a new project in db for the user's company
        /// </summary>
        /// <remakrs>
        /// Only Project Manager and Admin can create new project
        /// </remakrs>
        /// <param name="user">The current user's claims</param>
        /// <param name="project">The project to be saved in db</param>
        /// <retrun>The project after it has been saved in db</retrun>
        public Task<Project> CreateProjectAsync(Project project, UserInfo user);

        /// <summary>
        /// Update the details of a project in the database,
        /// as long as it exists for the user's company and
        /// they are authorized to do so
        /// </summary>
        /// <remarks>
        /// Users must be in the role of Admin or Project Manager.
        /// If the user is a Project Manager, they must be assigned
        /// to the project they are updating.
        /// </remarks>
        /// <param name="project">The updated project information</param>
        /// <param name="user">The current user's claims</param>
        public Task UpdateProjectAsync(Project project, UserInfo user);

        /// <summary>
        /// Archives a project to mark it as inactive. This method will also
        /// archive all of the tickets associated with the project.
        /// </summary>
        /// <remarks>
        /// Projects may only be archived by admins of the project's company
        /// or the project manager assigned to the project
        /// </remarks>
        /// <param name="projectId">The ID of the project to archive</param>
        /// <param name="user">The claims of the current user</param>
        public Task ArchiveProjectAsync(int projectId, UserInfo user);

        /// <summary>
        /// Restore a project to mark it as active. This method will also
        /// restore all of the tickets associated with the project that
        /// were not previously archived.
        /// </summary>
        /// <remarks>
        /// Projects may only be restored by admins of the project's company
        /// or the project manager assigned to the project
        /// </remarks>
        /// <param name="projectId">The ID of the project to restore</param>
        /// <param name="user">The claims of the current user</param>
        public Task RestoreProjectAsync(int projectId, UserInfo user);

        /// <summary>
        /// Assigns a user to the specified project,
        /// if they are not already assigned.
        /// </summary>
        /// <param name="projectId">The ID of the project</param>
        /// <param name="userId">The ID of the user</param>
        /// <param name="user">The current user's claims</param>
        public Task AddProjectMemberAsync(int projectId, string userId, UserInfo user);

        /// <summary>
        /// Removes a user to the specified project,
        /// if they are currently assigned to it.
        /// </summary>
        /// <param name="projectId">The ID of the project</param>
        /// <param name="userId">The ID of the user</param>
        /// <param name="user">The current user's claims</param>
        public Task RemoveProjectMemberAsync(int projectId, string userId, UserInfo user);

        /// <summary>
        /// Retrieves a list of users currently assigned
        /// to a specific project
        /// </summary>
        /// <param name="projectId">The ID of the project</param>
        /// <param name="user">The claims of the current user</param>
        /// <returns>A collection of users</returns>
        public Task<IEnumerable<ApplicationUser>> GetProjectMembersAsync(int projectId, UserInfo user);

        /// <summary>
        /// Retrieves the assigned project manager for a given project
        /// </summary>
        /// <param name="projectId">The ID of the project</param>
        /// <param name="userInfo">The claims of the current user</param>
        /// <returns>The assigned Project Manager, or null if none exists</returns>
        public Task<ApplicationUser?> GetProjectManagerAsync(int projectId, UserInfo userInfo);

        /// <summary>
        /// Assigns a project manager to a project. If another
        /// project manager was already assigned, they will be
        /// removed.
        /// </summary>
        /// <param name="projectId">The ID of the project</param>
        /// <param name="managerId">The ID of the project manager to assign</param>
        /// <param name="userInfo">The claims of the current user</param>
        /// <returns></returns>
        public Task AssignProjectManagerAsync(int projectId, string managerId, UserInfo userInfo);

        /// <summary>
        /// Removes the project manager from a project, if one
        /// is assigned
        /// </summary>
        /// <param name="projectId">The ID of the project</param>
        /// <param name="userInfo">The claims of the current user</param>
        public Task RemoveProjectManagerAsync(int projectId, UserInfo userInfo);
    }
}
