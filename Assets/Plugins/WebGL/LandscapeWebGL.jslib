mergeInto(LibraryManager.library, {
  InstallLandscapeWebGL: function () {
    if (window.__reelOfDungeonLandscapeInstalled) return;
    window.__reelOfDungeonLandscapeInstalled = true;

    var coarsePointer = window.matchMedia &&
      window.matchMedia('(pointer: coarse)').matches;
    var mobileUserAgent =
      /Android|iPhone|iPad|iPod|Mobile/i.test(navigator.userAgent || '');
    if (!coarsePointer && !mobileUserAgent) return;

    var overlay = document.createElement('div');
    overlay.id = 'reel-of-dungeon-landscape-guide';
    overlay.setAttribute('aria-label', 'Rotate device to landscape');
    overlay.style.cssText = [
      'position:fixed',
      'inset:0',
      'z-index:2147483647',
      'display:none',
      'align-items:center',
      'justify-content:center',
      'flex-direction:column',
      'gap:4vh',
      'background:#080b16',
      'color:#ffd21f',
      'font-family:monospace',
      'font-weight:900',
      'text-align:center',
      'touch-action:none'
    ].join(';');
    overlay.innerHTML =
      '<div style="font-size:22vw;line-height:1">&#8635;</div>' +
      '<div style="font-size:7vw;letter-spacing:.08em">ROTATE</div>';
    document.body.appendChild(overlay);

    var refresh = function () {
      var portrait = window.innerHeight > window.innerWidth;
      overlay.style.display = portrait ? 'flex' : 'none';
    };

    window.__reelOfDungeonRefreshOrientation = refresh;
    window.addEventListener('resize', refresh);
    window.addEventListener('orientationchange', function () {
      window.setTimeout(refresh, 120);
    });
    refresh();
  },

  LockLandscapeWebGL: function () {
    var lockOrientation = function () {
      if (screen.orientation && screen.orientation.lock) {
        var lockResult = screen.orientation.lock('landscape');
        if (lockResult && lockResult.catch) lockResult.catch(function () {});
      }
    };

    var root = document.documentElement;
    var requestFullscreen = root.requestFullscreen ||
      root.webkitRequestFullscreen ||
      root.msRequestFullscreen;

    if (!document.fullscreenElement && requestFullscreen) {
      try {
        var fullscreenResult = requestFullscreen.call(root);
        if (fullscreenResult && fullscreenResult.then) {
          fullscreenResult.then(lockOrientation).catch(lockOrientation);
        } else {
          lockOrientation();
        }
      } catch (error) {
        lockOrientation();
      }
    } else {
      lockOrientation();
    }

    if (window.__reelOfDungeonRefreshOrientation) {
      window.setTimeout(window.__reelOfDungeonRefreshOrientation, 150);
    }
  }
});
