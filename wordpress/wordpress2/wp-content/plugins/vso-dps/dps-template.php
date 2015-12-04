<?php
/*
Template Name: dps-template
*/

get_header(); 

if(function_exists('get_article')) {
    $publishedVersion = get_published_version();
?>
<script type='text/javascript'>
        // Apply TOC styles to nodes
        (function($){ 
            $(document).ready(function () {
                $('.TocNavigationVertical>ul').addClass('tocLevel1');
                $('.TocNavigationVertical>ul>li>ul').addClass('tocLevel2');
                $('.TocNavigationVertical>ul>li>a').addClass('normal');
                $('.TocNavigationVertical>ul>li>ul>li>a').addClass('normal');

                var arr = window.location.pathname.split('/');
                var articleUrl = '../' + arr[arr.length - 2] + '/';
                var active = $('.TocNavigationVertical>ul>li>ul>li>a[href=\"../build/\"]');

                var activeSection = active.closest('.TocNavigationVertical>ul>li');

                active.addClass('active').removeClass('normal');
                active.parent().parent().css({ display: 'block' });
                activeSection.children('a').addClass('active').removeClass('normal');

            });
        })(jQuery);
</script>    
<div id='doc-article' class='doc-article'>
    <div id='content'>
        <div class='simpleLeftNav'>
            <div class='leftNavigation'>
                <div class='TocNavigationVertical'>
<?=get_table_of_contents($publishedVersion); ?>
                </div>
            </div>
            <div class='mainContent'>
                <div id='oaContent'>
                    <div class='content'>
<?=get_article($publishedVersion, $wp_query->query_vars['documentation_id']); ?>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

<?php } ?>

