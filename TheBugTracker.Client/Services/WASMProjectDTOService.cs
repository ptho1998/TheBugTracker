using System.Net.Http.Json;
using TheBugTracker.Client.Models;
using TheBugTracker.Client.Models.Enums;
using TheBugTracker.Client.Services.Interfaces;
using TheBugTracker.Client.UserInfoHelper;

namespace TheBugTracker.Client.Services
{
    public class WASMProjectDTOService(HttpClient http) : IProjectDTOService
    {
        public async Task<IEnumerable<ProjectDTO>> GetProjectAsync(UserInfo user)
        {
            try
            {
                List<ProjectDTO> projects = await http.GetFromJsonAsync<List<ProjectDTO>>("api/Projects") ?? [];

                return projects;
            }
            catch (Exception ex) 
            { 
                Console.WriteLine(ex);
                return [];
            }
        }

        public async Task<IEnumerable<ProjectDTO>> GetArchivedProjectAsync(UserInfo user)
        {
            try
            {
                List<ProjectDTO> projects = await http.GetFromJsonAsync<List<ProjectDTO>>(
                    $"api/Projects?filter={ProjectsFilter.Archived}"
                    ) ?? [];

                return projects;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return [];
            }
        }

        public async Task<IEnumerable<ProjectDTO>> GetAssignedProjectAsync(UserInfo user)
        {
            try
            {
                List<ProjectDTO> projects = await http.GetFromJsonAsync<List<ProjectDTO>>(
                    $"api/Projects?filter={ProjectsFilter.Assigned}"
                    ) ?? [];

                return projects;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return [];
            }
        }

        public async Task<ProjectDTO?> GetProjectByIdAsync(int id, UserInfo user)
        {
            try
            {
                var project = await http.GetFromJsonAsync<ProjectDTO>($"api/Projects/{id}");

                return project;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                return null;
            }
        }

        public async Task<ProjectDTO> CreateProjectAsync(ProjectDTO project, UserInfo user)
        {
            var response = await http.PostAsJsonAsync("api/Projects", project);

            response.EnsureSuccessStatusCode();

            ProjectDTO createdProject = await 
                response.Content.ReadFromJsonAsync<ProjectDTO>() 
                ?? throw new HttpIOException(HttpRequestError.InvalidResponse);

            return createdProject;
        }

        public async Task UpdateProjectAsync(ProjectDTO project, UserInfo user)
        {
            var response = await http.PutAsJsonAsync($"api/Projects/{project.Id}", project);
            response.EnsureSuccessStatusCode();
        }

        public async Task ArchiveProjectAsync(int projectId, UserInfo user)
        {
            var response = await http.PatchAsync($"api/Projects/{projectId}/archive", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task RestoreProjectAsync(int projectId, UserInfo user)
        {
            var response = await http.PatchAsync($"api/Projects/{projectId}/restore", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task AddProjectMemberAsync(int projectId, string userId, UserInfo user)
        {
            var response = await http.PutAsync($"api/Projects/{projectId}/members/{userId}", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task RemoveProjectMemberAsync(int projectId, string userId, UserInfo user)
        {
            var response = await http.DeleteAsync($"api/Projects/{projectId}/members/{userId}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<IEnumerable<UserDTO>> GetProjectMembersAsync(int projectId, UserInfo user)
        {
            try
            {
                List<UserDTO> users = await http.GetFromJsonAsync<List<UserDTO>>($"/api/Projects/{projectId}/members") ?? [];
                return users;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return [];
            }
        }

        public async Task<UserDTO?> GetProjectManagerAsync(int projectId, UserInfo userInfo)
        {
            try
            {
                UserDTO? projectManager = await http.GetFromJsonAsync<UserDTO?>($"api/Projects/{projectId}/manager");
                return projectManager;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
 
        }

        public async Task AssignProjectManagerAsync(int projectId, string managerId, UserInfo userInfo)
        {
            var response = await http.PutAsync($"api/Projects/{projectId}/manager/{managerId}", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task RemoveProjectManagerAsync(int projectId, UserInfo userInfo)
        {
            var response = await http.DeleteAsync($"api/Projects/{projectId}/manager");
            response.EnsureSuccessStatusCode();
        }

    }
}
