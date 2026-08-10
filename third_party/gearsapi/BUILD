# Vendored GearsAPI / InControl for LimitByCraftingSkillMod Gears soft-dep.
# GearsAPI.dll: ship with the mod. InControl.dll: compile-only (game provides runtime).

load("@rules_dotnet_framework//dotnet:defs.bzl", "net_import_library")

filegroup(
    name = "gearsapi_dll",
    srcs = ["lib/net472/GearsAPI.dll"],
    visibility = ["//visibility:public"],
)

net_import_library(
    name = "gearsapi_net472_import",
    src = "lib/net472/GearsAPI.dll",
    version = "2.0.1.0",
    visibility = ["//visibility:public"],
)

net_import_library(
    name = "incontrol_net472_import",
    src = "lib/net472/InControl.dll",
    version = "0.0.0.0",
    visibility = ["//visibility:public"],
)
