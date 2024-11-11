using System.Collections.Generic;
using Content.Client.Fluids;
using Content.Server._Sunrise.ERP.Systems;
using Content.Shared._Sunrise.ERP;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Lust;

[TestFixture]
public sealed class LactationTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: FemaleDummy
  parent: MobHuman
  components:
  - type: HumanoidAppearance
    sex: Female";

    [Test]
    public async Task TestLactation()
    {
        await using var pair = await PoolManager.GetServerClient();
        var (server, client) = (pair.Server, pair.Client);
        var map = await pair.CreateTestMap();

        var entityManager = server.ResolveDependency<IEntityManager>();

        var interactionSystem = entityManager.System<InteractionSystem>();
        var puddleSystem = entityManager.System<PuddleSystem>();
        var solutionContainerSystem = entityManager.System<SharedSolutionContainerSystem>();

        EntityUid femaleUid = default!;
        EntityUid maleUid = default!;

        await server.WaitAssertion(
            () => femaleUid = server.EntMan.Spawn("FemaleDummy", new MapCoordinates(0, 0, map.MapId)));
        await server.WaitAssertion(
            () => maleUid = server.EntMan.Spawn("MobHuman", new MapCoordinates(0, 1, map.MapId)));

        Console.WriteLine($"femaleUid: {femaleUid}");
        Console.WriteLine($"maleUid: {maleUid}");

        await pair.RunTicksSync(5);

        var maleSolutionComponent = entityManager.GetComponent<SolutionContainerManagerComponent>(maleUid);
        var femaleSolutionComponent = entityManager.GetComponent<SolutionContainerManagerComponent>(femaleUid);

        var milkQuantity = FixedPoint2.New(5);
        var modifier = 0.2f;

        Assert.That(solutionContainerSystem.TryGetSolution(maleUid, "chemicals", out var maleSolutionEntity, out var maleSolution));
        Assert.That(solutionContainerSystem.TryGetSolution(femaleUid, "bloodstream", out var femaleSolutionEntity, out var femaleSolution));

        // puddleSystem.TrySplashSpillAt()

        // Первый тест. С соседом
        {
            var preBlood = femaleSolution.GetTotalPrototypeQuantity("Blood");

            interactionSystem.ProcessInteraction(entityManager.GetNetEntity(maleUid),
                entityManager.GetNetEntity(femaleUid),
                new InteractionPrototype()
                {
                    AmountLactate = milkQuantity,
                    Category = "грудь",
                    Coefficient = modifier,
                    Emotes = new HashSet<string>(),
                    Erp = true,
                    Icon = new SpriteSpecifier.Texture(new ResPath("_Sunrise/Interface/ERP/boobs_suck.png")),
                    InhandObject = new HashSet<string>(),
                    LactationSolution = null,
                    LactationStimulationFlag = true,
                    LovePercentTarget = 5,
                    LovePercentUser = 0,
                    Name = "test1",
                    UseSelf = false,
                    TargetSex = Sex.Female,
                    UserSex = Sex.Male,
                });
            await pair.RunTicksSync(5);

            var milk = maleSolution.GetTotalPrototypeQuantity("Milk");
            var postBlood = femaleSolution.GetTotalPrototypeQuantity("Blood");

            Assert.That(milk, Is.EqualTo(milkQuantity));
            Assert.That(preBlood - postBlood == milkQuantity * modifier);
        }

        // Второй тест. Без соседа
        {
            var preBlood = femaleSolution.GetTotalPrototypeQuantity("Blood");

            interactionSystem.ProcessInteraction(entityManager.GetNetEntity(femaleUid),
                entityManager.GetNetEntity(femaleUid),
                new InteractionPrototype()
                {
                    AmountLactate = milkQuantity,
                    Category = "грудь",
                    UseSelf = true,
                    Name = "test2",
                    LactationStimulationFlag = true,
                    LactationSolution = null,

                });
            await pair.RunTicksSync(5);
            var postBlood = femaleSolution.GetTotalPrototypeQuantity("Blood");
            Assert.That(preBlood - postBlood == milkQuantity * modifier);
        }

        await pair.CleanReturnAsync();
    }
}
