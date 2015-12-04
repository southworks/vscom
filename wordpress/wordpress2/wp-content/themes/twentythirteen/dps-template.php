<?php
/*
Template Name: dps-template
*/

get_header(); 

if(function_exists('get_article')) {
    $stringContent = get_article($wp_query->query_vars['documentation_id']);
    print_r($stringContent);
}

?>
