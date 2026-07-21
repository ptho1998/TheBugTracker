using Microsoft.AspNetCore.Identity;
using TheBugTracker.Client.Models;
using TheBugTracker.Client.Models.Enums;
using TheBugTracker.Client.Services.Interfaces;
using TheBugTracker.Client.UserInfoHelper;
using TheBugTracker.Models;
using TheBugTracker.Services.Interfaces;

namespace TheBugTracker.Services
{
    public class ProjectDTOService (IProjectRepository repository,  
                                    UserManager<ApplicationUser> userManager) : IProjectDTOService
    {
        public async Task<IEnumerable<ProjectDTO>> GetProjectAsync(UserInfo user)
        {
            IEnumerable<Project> projects = await repository.GetProjectAsync(user);

            IEnumerable<ProjectDTO> dtos = projects.Select(p => p.ToDTO());

            return dtos;
        }

        public async Task<IEnumerable<ProjectDTO>> GetArchivedProjectAsync(UserInfo user)
        {
            IEnumerable<Project> projects = await repository.GetArchivedProjectAsync(user);

            IEnumerable<ProjectDTO> dtos = projects.Select(p => p.ToDTO());

            return dtos;
        }

        public async Task<IEnumerable<ProjectDTO>> GetAssignedProjectAsync(UserInfo user)
        {
            IEnumerable<Project> projects = await repository.GetAssignedProjectAsync(user);

            IEnumerable<ProjectDTO> dtos = projects.Select(p => p.ToDTO());

            return dtos;
        }

        public async Task<ProjectDTO?> GetProjectByIdAsync(int id, UserInfo user)
        {
            Project? project = await repository.GetProjectByIdAsync(id, user);
            if (project is null) return null;

            List<UserDTO> members = [];

            foreach (ApplicationUser member in project.Members)
            {
                UserDTO dto = await member.ToDTOWithRole(userManager);
                members.Add(dto);
            }

            ProjectDTO projectDTO = project.ToDTO();

            projectDTO.Members = members;

            return projectDTO;
        }

        public async Task<ProjectDTO> CreateProjectAsync(ProjectDTO project, UserInfo user)
        {
            Project dbProject = new()
            {
                Name = project.Name,
                Description = project.Description,
                Created = DateTimeOffset.UtcNow,
                StartDate = project.StartDate ?? DateTimeOffset.UtcNow,
                EndDate = project.EndDate ?? DateTimeOffset.UtcNow + TimeSpan.FromDays(7),
                Priority = project.Priority,
                Archived = false,
                CompanyId = user.CompanyId,
            };

            dbProject = await repository.CreateProjectAsync(dbProject, user);

            return dbProject.ToDTO();
        }

        public async Task UpdateProjectAsync(ProjectDTO project, UserInfo user)
        {
            Project? dbProject = await repository.GetProjectByIdAsync(project.Id, user);

            if (dbProject is null)
            {
                return;
            }

            dbProject.Name = project.Name;
            dbProject.Description = project.Description;
            dbProject.StartDate = project.StartDate ?? dbProject.StartDate;
            dbProject.EndDate = project.EndDate ?? dbProject.EndDate;
            dbProject.Priority = project.Priority;

            await repository.UpdateProjectAsync(dbProject, user);
        }

        public async Task ArchiveProjectAsync(int projectId, UserInfo user)
        {
            await repository.ArchiveProjectAsync(projectId, user);
        }

        public async Task RestoreProjectAsync(int projectId, UserInfo user)
        {
            await repository.RestoreProjectAsync(projectId, user);
        }

        public async Task AddProjectMemberAsync(int projectId, string userId, UserInfo user)
        {
            await repository.AddProjectMemberAsync(projectId, userId, user);
        }

        public async Task RemoveProjectMemberAsync(int projectId, string userId, UserInfo user)
        {
            await repository.RemoveProjectMemberAsync(projectId, userId, user);
        }

        public async Task<IEnumerable<UserDTO>> GetProjectMembersAsync(int projectId, UserInfo user)
        {
            IEnumerable<ApplicationUser> members = await repository.GetProjectMembersAsync(projectId, user);

            List<UserDTO> dtos = [];

            foreach(ApplicationUser member in members)
            {
                UserDTO dto = await member.ToDTOWithRole(userManager);
                dtos.Add(dto);

            }

            return dtos;
        }

        public async Task<UserDTO?> GetProjectManagerAsync(int projectId, UserInfo userInfo)
        {
            ApplicationUser? projectManager = await repository.GetProjectManagerAsync(projectId, userInfo);

            if (projectManager is null)
            {
                return null;
            }

            UserDTO dto = projectManager.ToDTO();
            dto.Role = Role.ProjectManager;

            return dto;
        }

        public async Task AssignProjectManagerAsync(int projectId, string managerId, UserInfo userInfo)
        {
            await repository.AssignProjectManagerAsync(projectId, managerId, userInfo);
        }

        public async Task RemoveProjectManagerAsync(int projectId, UserInfo userInfo)
        {
            await repository.RemoveProjectManagerAsync(projectId, userInfo);
        }


    }
}
