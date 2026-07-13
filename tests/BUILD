load("@rules_dotnet_framework//dotnet:defs.bzl", "net_xunit_test")

UNIT_TESTS = [
    ":game_reflection",
    ":limit_logic",
    ":mod_config",
]

TOOL_TESTS = [
    ":deploy_tool",
]

ALL_TESTS = UNIT_TESTS + TOOL_TESTS

XUNIT_DEPS = [
    "@rules_dotnet_framework//dotnet/stdlib.net/net472:system.collections.concurrent.dll",
    "@rules_dotnet_framework//dotnet/stdlib.net/net472:system.collections.dll",
    "@rules_dotnet_framework//dotnet/stdlib.net/net472:system.objectmodel.dll",
    "@rules_dotnet_framework//dotnet/stdlib.net/net472:system.runtime.dll",
    "@rules_dotnet_framework//dotnet/stdlib.net/net472:system.threading.tasks.dll",
    "@xunit_assert//:net",
    "@xunit_extensibility_core//:net",
    "@xunit_extensibility_execution//:net",
]

MOD_TEST_DEPS = XUNIT_DEPS + [
    "//src:LimitByCraftingSkillMod",
]

net_xunit_test(
    name = "game_reflection",
    size = "small",
    srcs = [
        "GameReflectionTests.cs",
        "RequiredLevelFromProgressionTests.cs",
    ],
    deps = MOD_TEST_DEPS,
    dotnet_context_data = "@rules_dotnet_framework//:net_context_data_net472",
    testlauncher = "@xunit_runner_console//:net",
)

net_xunit_test(
    name = "limit_logic",
    size = "small",
    srcs = ["LimitByCraftingSkillLogicTests.cs"],
    deps = MOD_TEST_DEPS,
    dotnet_context_data = "@rules_dotnet_framework//:net_context_data_net472",
    testlauncher = "@xunit_runner_console//:net",
)

net_xunit_test(
    name = "mod_config",
    size = "small",
    srcs = ["ModConfigTests.cs"],
    deps = MOD_TEST_DEPS,
    dotnet_context_data = "@rules_dotnet_framework//:net_context_data_net472",
    testlauncher = "@xunit_runner_console//:net",
)

net_xunit_test(
    name = "progression_assembly_integration",
    size = "small",
    srcs = ["ProgressionAndAssemblyIntegrationTests.cs"],
    deps = XUNIT_DEPS + [
        "@rules_dotnet_framework//dotnet/stdlib.net/net472:system.xml.dll",
    ],
    dotnet_context_data = "@rules_dotnet_framework//:net_context_data_net472",
    testlauncher = "@xunit_runner_console//:net",
)


net_xunit_test(
    name = "deploy_tool",
    size = "small",
    srcs = [
        "DeployToolTests.cs",
        "TestDataPath.cs",
    ],
    data = ["//tools:deploy.ps1"],
    deps = XUNIT_DEPS,
    dotnet_context_data = "@rules_dotnet_framework//:net_context_data_net472",
    testlauncher = "@xunit_runner_console//:net",
)


net_xunit_test(
    name = "restriction_scan_benchmark",
    size = "small",
    srcs = [
        "RestrictionScanBenchmarkTests.cs",
        "RequiredLevelFromProgressionTests.cs",
    ],
    tags = ["manual"],
    deps = MOD_TEST_DEPS,
    dotnet_context_data = "@rules_dotnet_framework//:net_context_data_net472",
    testlauncher = "@xunit_runner_console//:net",
)

test_suite(
    name = "unit",
    tests = UNIT_TESTS,
)

test_suite(
    name = "all",
    tests = ALL_TESTS,
)
