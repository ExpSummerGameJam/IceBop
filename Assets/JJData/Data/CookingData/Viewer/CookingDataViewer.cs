using UnityEngine;




public  class CookingDataViewer : MonoBehaviour
{
    public CookingData_IngredientCategories[] IngredientCategoriesDataViewer;
    public CookingData_Ingredients[] IngredientsDataViewer;
    public CookingData_AbilityDefinitions[] AbilityDefinitionsDataViewer;
    public CookingData_Dishes[] DishesDataViewer;
    public CookingData_Recipes[] RecipesDataViewer;
    public CookingData_RecipeSteps[] RecipeStepsDataViewer;
    void Start()
    {
        IngredientCategoriesDataViewer = CookingData.IngredientCategories;
        IngredientsDataViewer = CookingData.Ingredients;
        AbilityDefinitionsDataViewer = CookingData.AbilityDefinitions;
        DishesDataViewer = CookingData.Dishes;
        RecipesDataViewer = CookingData.Recipes;
        RecipeStepsDataViewer = CookingData.RecipeSteps;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateViewer()
    {
        GameObject viewer = new GameObject($"CookingDataViewer");
        viewer.AddComponent<CookingDataViewer>();
        GameObject.DontDestroyOnLoad(viewer);
    }
}
