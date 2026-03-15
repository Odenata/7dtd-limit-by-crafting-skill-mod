def _xunit_runner_console_repo_impl(ctx):
    ctx.download_and_extract(
        url = "https://www.nuget.org/api/v2/package/xunit.runner.console/2.5.3",
        output = "",
        sha256 = "78d8b7d7b1279232f80965603ca1d872f0f5f13acf5243985bcdc1d50e1c28be",
        type = "zip",
    )
    ctx.file(
        "BUILD.bazel",
        """package(default_visibility = ["//visibility:public"])

load("@rules_dotnet_framework//dotnet:defs.bzl", "net_import_binary", "net_import_library")

net_import_library(
    name = "xunit_abstractions",
    src = "tools/net472/xunit.abstractions.dll",
    version = "2.5.3",
)

net_import_library(
    name = "xunit_runner_reporters",
    src = "tools/net472/xunit.runner.reporters.net452.dll",
    version = "2.5.3",
)

net_import_library(
    name = "xunit_runner_utility",
    src = "tools/net472/xunit.runner.utility.net452.dll",
    version = "2.5.3",
)

net_import_binary(
    name = "net",
    src = "tools/net472/xunit.console.exe",
    version = "2.5.3",
    deps = [
        ":xunit_abstractions",
        ":xunit_runner_reporters",
        ":xunit_runner_utility",
    ],
    data = [
        "tools/net472/xunit.console.exe.config",
        "tools/net472/xunit.console.x86.exe",
        "tools/net472/xunit.console.x86.exe.config",
    ],
)
""",
    )

xunit_runner_console_repo = repository_rule(
    implementation = _xunit_runner_console_repo_impl,
)
