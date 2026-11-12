/* Used by user and role permission settings.
 *
 * The permission list is grouped module-wise (see AppAuthorizationProvider), which makes it far too
 * long to scroll through fully expanded. So the tree opens with only the module groups visible and
 * offers a search box plus expand/collapse and select/deselect shortcuts.
 */
appModule.directive('permissionTree', [
      function () {
          return {
              restrict: 'E',
              template:
                  '<div class="permission-tree-wrapper">' +
                  '  <div class="permission-tree-toolbar">' +
                  '    <div class="input-group permission-tree-search">' +
                  '      <span class="input-group-addon"><i class="fa fa-search"></i></span>' +
                  '      <input type="text" class="form-control" ng-model="searchText" ng-change="onSearch()" ng-model-options="{debounce: 200}" placeholder="{{::searchPlaceholder}}">' +
                  '      <span class="input-group-btn">' +
                  '        <button type="button" class="btn btn-default" ng-click="clearSearch()" ng-disabled="!searchText"><i class="fa fa-times"></i></button>' +
                  '      </span>' +
                  '    </div>' +
                  '    <div class="btn-group btn-group-sm permission-tree-actions">' +
                  '      <button type="button" class="btn btn-default" ng-click="expandAll()">{{::expandAllText}}</button>' +
                  '      <button type="button" class="btn btn-default" ng-click="collapseAll()">{{::collapseAllText}}</button>' +
                  '      <button type="button" class="btn btn-default" ng-click="selectAll()">{{::selectAllText}}</button>' +
                  '      <button type="button" class="btn btn-default" ng-click="deselectAll()">{{::deselectAllText}}</button>' +
                  '    </div>' +
                  '    <span class="permission-tree-count badge">{{selectedCount}}</span>' +
                  '  </div>' +
                  '  <div class="permission-tree"></div>' +
                  '</div>',
              scope: {
                  editData: '='
              },
              link: function ($scope, element, attr) {

                  var $tree = $(element).find('.permission-tree');
                  var treeInitializedBefore = false;
                  var inTreeChangeEvent = false;

                  $scope.searchText = '';
                  $scope.selectedCount = 0;
                  $scope.searchPlaceholder = app.localize('SearchPermissions');
                  $scope.expandAllText = app.localize('ExpandAll');
                  $scope.collapseAllText = app.localize('CollapseAll');
                  $scope.selectAllText = app.localize('SelectAll');
                  $scope.deselectAllText = app.localize('DeselectAll');

                  $scope.$watch('editData', function () {
                      if (!$scope.editData) {
                          return;
                      }

                      initializeTree();
                  });

                  function initializeTree() {
                      if (treeInitializedBefore) {
                          $tree.jstree('destroy');
                      }

                      //A permission whose parent is not in the list (e.g. the parent is not available to
                      //this tenant) would make jstree throw and leave the tree stuck on "Loading ...".
                      //Such items are shown as roots instead, so they stay visible and keep their grant.
                      var knownNames = {};
                      _.each($scope.editData.permissions, function (item) {
                          knownNames[item.name] = true;
                      });

                      var treeData = _.map($scope.editData.permissions, function (item) {
                          var hasParent = item.parentName && knownNames[item.parentName];
                          return {
                              id: item.name,
                              parent: hasParent ? item.parentName : '#',
                              text: item.displayName,
                              state: {
                                  //Only the roots start open - module groups stay collapsed so the
                                  //tree fits on screen. Search and "Expand All" open them on demand.
                                  opened: !hasParent,
                                  selected: _.contains($scope.editData.grantedPermissionNames, item.name)
                              }
                          };
                      });

                      $tree.jstree({
                          'core': {
                              data: treeData,
                              themes: {
                                  dots: false
                              }
                          },
                          "types": {
                              "default": {
                                  "icon": "fa fa-folder tree-item-icon-color icon-lg"
                              },
                              "file": {
                                  "icon": "fa fa-file tree-item-icon-color icon-lg"
                              }
                          },
                          'checkbox': {
                              keep_selected_style: false,
                              three_state: false,
                              cascade: ''
                          },
                          'search': {
                              show_only_matches: true,
                              show_only_matches_children: true,
                              case_sensitive: false
                          },
                          plugins: ['checkbox', 'types', 'search']
                      });

                      treeInitializedBefore = true;

                      //Counted from the data, not from the tree: syncing the scope here would
                      //overwrite grantedPermissionNames before the user has changed anything, and
                      //would drop any granted permission that the tree does not render.
                      $scope.selectedCount = _.filter(treeData, function (node) {
                          return node.state.selected;
                      }).length;

                      $tree.on("changed.jstree", function (e, data) {
                          var wasInTreeChangeEvent = inTreeChangeEvent;
                          if (!wasInTreeChangeEvent) {
                              inTreeChangeEvent = true;
                          }

                          //data.node is undefined for bulk actions (select_all/deselect_all);
                          //those need no cascading, only the scope sync below.
                          if (data.node) {
                              var childrenNodes;

                              if (data.node.state.selected) {
                                  selectNodeAndAllParents($tree.jstree('get_parent', data.node));

                                  childrenNodes = $.makeArray($tree.jstree('get_children_dom', data.node));
                                  $tree.jstree('select_node', childrenNodes);

                              } else {
                                  childrenNodes = $.makeArray($tree.jstree('get_children_dom', data.node));
                                  $tree.jstree('deselect_node', childrenNodes);
                              }
                          }

                          if (!wasInTreeChangeEvent) {
                              inTreeChangeEvent = false;
                              syncScope();
                          }
                      });
                  };

                  function selectNodeAndAllParents(node) {
                      $tree.jstree('select_node', node, true);
                      var parent = $tree.jstree('get_parent', node);
                      if (parent) {
                          selectNodeAndAllParents(parent);
                      }
                  };

                  function getSelectedPermissionNames() {
                      var permissionNames = [];

                      var selectedPermissions = $tree.jstree('get_selected', true);
                      for (var i = 0; i < selectedPermissions.length; i++) {
                          permissionNames.push(selectedPermissions[i].original.id);
                      }

                      return permissionNames;
                  };

                  //The tree fires its events outside a digest, but the toolbar buttons fire inside
                  //one - so guard $apply instead of calling it unconditionally.
                  function syncScope() {
                      var names = getSelectedPermissionNames();

                      var assign = function () {
                          $scope.editData.grantedPermissionNames = names;
                          $scope.selectedCount = names.length;
                      };

                      var phase = $scope.$root && $scope.$root.$$phase;
                      if (phase === '$apply' || phase === '$digest') {
                          assign();
                      } else {
                          $scope.$apply(assign);
                      }
                  };

                  function rootNodeIds() {
                      return $tree.jstree('get_node', '#').children || [];
                  };

                  $scope.onSearch = function () {
                      if (!treeInitializedBefore) {
                          return;
                      }

                      $tree.jstree('search', $scope.searchText || '');
                  };

                  $scope.clearSearch = function () {
                      $scope.searchText = '';
                      $scope.onSearch();
                  };

                  $scope.expandAll = function () {
                      $tree.jstree('open_all');
                  };

                  $scope.collapseAll = function () {
                      $tree.jstree('close_all');
                      //Keep the roots open, otherwise the panel looks empty.
                      $tree.jstree('open_node', rootNodeIds());
                  };

                  $scope.selectAll = function () {
                      $tree.jstree('select_all');
                  };

                  $scope.deselectAll = function () {
                      $tree.jstree('deselect_all');
                  };
              }
          };
      }]);
