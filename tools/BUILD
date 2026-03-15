load("@seven_days_to_die_mod_dev_tools//tools/_shared:powershell_rules.bzl", "powershell_binary")

exports_files([
    "build.ps1",
    "deploy.ps1",
    "test-integration.ps1",
])

filegroup(
    name = "build_run_script",
    srcs = ["build_run.ps1"],
    visibility = ["//visibility:private"],
)

filegroup(
    name = "deploy_run_script",
    srcs = ["deploy_run.ps1"],
    visibility = ["//visibility:private"],
)

powershell_binary(
    name = "build",
    script = ":build_run_script",
    tags = ["local", "manual", "no-remote", "requires-mod-repo"],
    visibility = ["//visibility:public"],
)

powershell_binary(
    name = "deploy",
    script = ":deploy_run_script",
    tags = ["integration", "local", "manual", "no-remote", "requires-game", "requires-mod-repo"],
    visibility = ["//visibility:public"],
)
