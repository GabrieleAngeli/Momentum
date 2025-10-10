from tools.release_notes.versioning import (
    CommitMetadata,
    VersionBump,
    detect_bump_from_commit,
    determine_required_bump,
    plan_next_version,
)


def make_commit(message: str, files=None) -> CommitMetadata:
    return CommitMetadata(sha="deadbeef", message=message, files=files or [])


def test_detect_bump_for_breaking_change_marker():
    bump = detect_bump_from_commit("feat!: remove deprecated endpoint", ["src/service/file.cs"])
    assert bump == VersionBump.MAJOR


def test_detect_bump_for_proto_change():
    bump = detect_bump_from_commit("docs: update contract", ["contracts/customer/v1/customer.proto"])
    assert bump == VersionBump.MAJOR


def test_detect_bump_for_feature_commit():
    bump = detect_bump_from_commit("feat: add new workflow", ["src/app/file.cs"])
    assert bump == VersionBump.MINOR


def test_detect_bump_for_fix_commit_defaults_to_patch():
    bump = detect_bump_from_commit("fix: correct configuration", ["src/app/file.cs"])
    assert bump == VersionBump.PATCH


def test_determine_required_bump_returns_highest_level():
    commits = [
        make_commit("fix: adjust retry policy", ["src/app/a.cs"]),
        make_commit("feat: new API", ["src/app/b.cs"]),
    ]
    bump = determine_required_bump(commits)
    assert bump == VersionBump.MINOR


def test_plan_next_version_from_initial_state_minor():
    commits = [make_commit("feat: something", ["src/app/a.cs"])]
    version, bump = plan_next_version(current_version=None, commits=commits)
    assert version == "0.1.0"
    assert bump == VersionBump.MINOR


def test_plan_next_version_patch_increment():
    commits = [make_commit("fix: bug", ["src/app/a.cs"])]
    version, bump = plan_next_version(current_version="1.2.3", commits=commits)
    assert version == "1.2.4"
    assert bump == VersionBump.PATCH
