using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orchard.Environment.Extensions;
using Orchard.Recipes.Models;

namespace Orchard.Recipes.Services
{
    public class RecipeHarvester : IRecipeHarvester
    {
        private readonly IExtensionManager _extensionManager;
        private readonly IHostingEnvironment _hostingEnvironment;

        public RecipeHarvester(IExtensionManager extensionManager,
            IHostingEnvironment hostingEnvironment,
            IStringLocalizer<RecipeHarvester> localizer,
            ILogger<RecipeHarvester> logger)
        {
            _extensionManager = extensionManager;
            _hostingEnvironment = hostingEnvironment;

            T = localizer;
            Logger = logger;
        }

        public IStringLocalizer T { get; set; }
        public ILogger Logger { get; set; }

        public Task<IEnumerable<RecipeDescriptor>> HarvestRecipesAsync(string subPath)
        {
            return Task.FromResult(HarvestRecipes(subPath));
        }

        private IEnumerable<RecipeDescriptor> HarvestRecipes(string subPath)
        {
            var folderSubPath = Path.Combine(subPath, "recipes");
            var recipeContainerFileInfo = _hostingEnvironment
                .ContentRootFileProvider
                .GetFileInfo(folderSubPath);
            
            var matcher = new Matcher(System.StringComparison.OrdinalIgnoreCase);
            matcher.AddInclude("*.recipe.json");

            return matcher
                .Execute(new DirectoryInfoWrapper(new DirectoryInfo(recipeContainerFileInfo.PhysicalPath)))
                .Files
                .Select(match =>
                {
                    var recipeFile = _hostingEnvironment
                        .ContentRootFileProvider
                        .GetFileInfo(Path.Combine(folderSubPath, match.Path));

                    // TODO: Try to optimize by only reading the required metadata instead of the whole file
                    using (var file = new StreamReader(recipeFile.CreateReadStream()))
                    {
                        using (var reader = new JsonTextReader(file))
                        {
                            var serializer = new JsonSerializer();
                            var recipeDescriptor = serializer.Deserialize<RecipeDescriptor>(reader);
                            recipeDescriptor.RecipeFileInfo = recipeFile;
                            return recipeDescriptor;
                        }
                    }
                });
        }
    }
}