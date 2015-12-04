<?php
/*
Template Name: dps-list-template
*/

get_header(); 

if(function_exists('get_document_list')) {
    $stringContent = get_document_list();
    print_r($stringContent);
}

?>
