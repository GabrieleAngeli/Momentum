from subprocess import CalledProcessError

from tools.release_notes import generate_release_notes as grn


def test_determine_commits_falls_back_when_base_missing(monkeypatch, capsys):
    calls = []

    def fake_run(args, cwd=None):
        calls.append(tuple(args))
        if args[:2] == ["rev-parse", "--verify"]:
            raise CalledProcessError(returncode=128, cmd=["git", *args])
        if args[0] == "rev-list":
            return "c3\nc2\nc1"
        raise AssertionError(f"Unexpected git command: {args}")

    monkeypatch.setattr(grn, "run_git", fake_run)

    commits = grn.determine_commits("0.0.0", "HEAD")

    assert ("rev-parse", "--verify", "0.0.0^{commit}") in calls
    assert ("rev-list", "HEAD") in calls
    assert commits == ["c1", "c2", "c3"]

    captured = capsys.readouterr()
    assert "0.0.0" in captured.err


def test_determine_commits_with_existing_base(monkeypatch, capsys):
    def fake_run(args, cwd=None):
        if args == ["rev-parse", "--verify", "v1.0.0^{commit}"]:
            return "v1.0.0"
        if args == ["rev-list", "v1.0.0..HEAD"]:
            return "c2\nc1"
        raise AssertionError(f"Unexpected git command: {args}")

    monkeypatch.setattr(grn, "run_git", fake_run)

    commits = grn.determine_commits("v1.0.0", "HEAD")

    assert commits == ["c1", "c2"]
    captured = capsys.readouterr()
    assert captured.err == ""
