load("@rules_python//python:py_test.bzl", "py_test")

py_test(
    name = "generate_classname_map_report_test",
    size = "small",
    srcs = ["generate_classname_map_report_test.py"],
    deps = ["//tools:generate_classname_map_report_lib"],
    data = [
        "//tests/fixtures:items_map_generator_sample.xml",
        "//tests/fixtures:progression_map_generator_sample.xml",
    ],
)
