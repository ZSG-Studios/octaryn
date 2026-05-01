using Octaryn.Shared.ApiExposure;
using Octaryn.Shared.FrameworkAllowlist;
using Octaryn.Shared.GameModules;

namespace Octaryn.Basegame.Module;

public sealed class BasegameModuleRegistration : IGameModuleRegistration
{
    public GameModuleManifest Manifest { get; } = new(
        ModuleId: "octaryn.basegame",
        DisplayName: "Octaryn Basegame",
        Version: "0.1.0",
        OctarynApiVersion: "0.1.0",
        RequiredCapabilities:
        [
            ModuleCapabilityIds.ContentBlocks,
            ModuleCapabilityIds.ContentItems,
            ModuleCapabilityIds.GameplayInteractions,
            ModuleCapabilityIds.GameplayRules
        ],
        RequestedHostApis:
        [
            HostApiIds.Commands,
            HostApiIds.Frame
        ],
        RequestedRuntimePackages:
        [
            AllowedPackageIds.Arch,
            AllowedPackageIds.ArchSystem,
            AllowedPackageIds.ArchEventBus,
            AllowedPackageIds.ArchRelationships
        ],
        RequestedBuildPackages:
        [
            AllowedPackageIds.ArchSystemSourceGenerator
        ],
        RequestedFrameworkApiGroups:
        [
            FrameworkApiGroupIds.BclPrimitives,
            FrameworkApiGroupIds.BclCollections,
            FrameworkApiGroupIds.BclMemory,
            FrameworkApiGroupIds.BclMath,
            FrameworkApiGroupIds.BclTime,
            FrameworkApiGroupIds.BclText
        ],
        ModuleDependencies: [],
        ContentDeclarations:
        [
            new GameModuleContentDeclaration(
                "octaryn.basegame.block.air",
                "block",
                "Data/Blocks/octaryn.basegame.block.air.json"),
            new GameModuleContentDeclaration(
                "octaryn.basegame.item.hand",
                "item",
                "Data/Items/octaryn.basegame.item.hand.json"),
            new GameModuleContentDeclaration(
                "octaryn.basegame.rule.default_interaction",
                "rule",
                "Data/Rules/octaryn.basegame.rule.default_interaction.json")
        ],
        AssetDeclarations:
        [
            new GameModuleAssetDeclaration(
                "octaryn.basegame.texture.placeholder",
                "texture",
                "Assets/Textures/octaryn.basegame.texture.placeholder.txt")
        ],
        Schedule: new GameModuleScheduleDeclaration(
        [
            BasegameScheduleDeclarations.FrameTick
        ]),
        Compatibility: new GameModuleCompatibility(
            MinimumHostApiVersion: "0.1.0",
            MaximumHostApiVersion: "0.1.0",
            SaveCompatibilityId: "octaryn.basegame.save.v0",
            SupportsMultiplayer: false));

    public IGameModuleInstance CreateInstance(ModuleHostContext context)
    {
        return GameContext.Create(context);
    }
}
