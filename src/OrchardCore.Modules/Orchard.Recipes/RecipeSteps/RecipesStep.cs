using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orchard.Environment.Extensions;
using Orchard.Recipes.Models;
using Orchard.Recipes.Services;

namespace Orchard.Recipes.RecipeSteps
{
    /// <summary>
    /// This recipe step executes a set of external recipes.
    /// </summary>
    public class RecipesStep : IRecipeStepHandler
    {
        private readonly IRecipeHarvester _recipeHarvester;
        private readonly IRecipeExecutor _recipeManager;
        private readonly IExtensionManager _extensionManager;

        public RecipesStep(
            IRecipeHarvester recipeHarvester,
            IRecipeExecutor recipeManager,
            IExtensionManager extensionManager)
        {
            _recipeHarvester = recipeHarvester;
            _recipeManager = recipeManager;
            _extensionManager = extensionManager;
        }

        /*
         {
            "name": "recipes",
            "recipes": [
                { "executionid": "Orchard.Setup", name="Core" }
            ]
         }
        */
        public async Task ExecuteAsync(RecipeExecutionContext context)
        {
            if (!String.Equals(context.Name, "Recipes", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var step = context.Step.ToObject<InternalStep>();
            var recipesDictionary = new Dictionary<string, IDictionary<string, RecipeDescriptor>>();

            foreach (var recipe in step.Values)
            {
                IDictionary<string, RecipeDescriptor> recipes;
                
                if (!recipesDictionary.TryGetValue(recipe.ExecutionId, out recipes))
                {
                    var extension = _extensionManager.GetExtension(recipe.ExecutionId);
                    recipes = (await _recipeHarvester.HarvestRecipesAsync(extension.SubPath)).ToDictionary(x => x.Name);
                    recipesDictionary[recipe.ExecutionId] = recipes;
                }

                if (!recipes.ContainsKey(recipe.Name))
                {
                    throw new ArgumentException($"No recipe named '{recipe.Name}' was found in extension '{recipe.ExecutionId}'.");
                }

                var executionId = Guid.NewGuid().ToString();
                await _recipeManager.ExecuteAsync(executionId, recipes[recipe.Name], context.Environment);
            }
        }

        private class InternalStep
        {
            public InternalStepValue[] Values { get; set; }
        }

        private class InternalStepValue
        {
            public string ExecutionId { get; set; }
            public string Name { get; set; }
        }
    }
}
