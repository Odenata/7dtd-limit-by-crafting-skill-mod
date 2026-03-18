load("@rules_python//python:py_binary.bzl", "py_binary")
load("@rules_python//python:py_library.bzl", "py_library")
load("@seven_days_to_die_mod_dev_tools//tools/_shared:powershell_rules.bzl", "powershell_binary")

py_library(
    name = "generate_classname_map_report_lib",
    srcs = ["generate_classname_map_report.py"],
    visibility = ["//tests/map_generator:__pkg__"],
)

py_binary(
    name = "generate_classname_map_report",
    srcs = ["generate_classname_map_report.py"],
    main = "generate_classname_map_report.py",
    visibility = ["//visibility:public"],
)

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
