<?php
    /*
    Plugin Name: DPS Web Experience for Wordpress
    Description: This WordPress plugin allows you to display articles published by DPS in a WordPress page.
    Version: 1.0
    Author: Microsoft
    */    

    require_once(dirname(__FILE__) . '/admin/vso-dps-admin.php');

    if(!class_exists('DPS_WordPress_Plugin'))
    {
        class DPS_WordPress_Plugin
        {            
            function register_styles() {
                wp_register_style('vso-dps', plugins_url( 'vso-dps/css/vso-dps-styles.css' ));
                wp_enqueue_style('vso-dps');
            }           

            function register_plugin_scripts() {
                wp_enqueue_script('jquery');
            }
            
            function register_admin_page() {
                add_options_page('DPS Web Experience for WordPress', 'DPS', 'administrator','vso-dps', 'vso_dps_admin_html_page');
            }

            function add_documentation_id_rewrite_tag() {
                add_rewrite_tag('%documentation_id%', '([^&]+)');
            }

            function add_documentation_pages_rewrite_rule() {
                $page = get_page_by_title("articles");
                add_rewrite_rule('^documentation/([^/]*)/?$', 'index.php?page_id=' . $page->ID . '&documentation_id=$matches[1]','top');
            }

            function add_dps_page_template_filter( $page_template ) {
                if ( is_page( 'articles' ) ) {
                    $page_template = plugin_dir_path( __FILE__ ) . 'dps-template.php';
                }
                return $page_template;
            }
            
            function add_dps_docs_template_filter( $page_template ) {
                if ( is_page( 'docs' ) ) {
                    $page_template = plugin_dir_path( __FILE__ ) . 'dps-list-template.php';                       
                }
                return $page_template;
            }
    
            public function bootstrap() {
                 // Installation and uninstallation hooks
                register_activation_hook(__FILE__ , array($this, 'activate'));
                register_deactivation_hook(__FILE__ , array($this, 'deactivate'));

                add_action('init', array($this,'add_documentation_id_rewrite_tag'), 10, 0);
                add_action('init', array($this,'add_documentation_pages_rewrite_rule') ); 
                
                add_action( 'admin_menu' , array($this,'register_admin_page') );        

                // Register style sheet
                add_action( 'wp_enqueue_scripts' , array($this,'register_plugin_scripts') );
                
                // Register page templates
                add_filter( 'page_template' , array($this, 'add_dps_page_template_filter') );
                add_filter( 'page_template', array($this, 'add_dps_docs_template_filter') );   
                
                // Register style sheet
                add_action( 'wp_enqueue_scripts' , array($this, 'register_styles') );                    
            }

            public function activate() {
                // Register options and menu
                add_option( 'vso_dps_storage_connection_string' );
                add_option( 'vso_dps_storage_container_name' );                

                $this->add_documentation_id_rewrite_tag();
                $this->add_documentation_pages_rewrite_rule();

                flush_rewrite_rules();
            } 
        
            public function deactivate() {
               // delete_option('vso_dps_storage_connection_string');
               // delete_option('vso_dps_storage_container_name');
            } 
        }
    } 

    if(class_exists('DPS_WordPress_Plugin')) {       
        // instantiate the plugin class
        $dps_wordpress_plugin = new DPS_WordPress_Plugin();
        $dps_wordpress_plugin->bootstrap();
    }     
    
    require_once "library/WindowsAzure/WindowsAzure.php";
    
    use WindowsAzure\Common\ServicesBuilder;
    use WindowsAzure\Common\ServiceException;
	use WindowsAzure\Blob\Models\ListBlobsOptions;
    
    function get_published_version() {
        $connectionString = get_option('vso_dps_storage_connection_string');
        $containerName = get_option('vso_dps_storage_container_name');
        $blobUrl = "en-us/settings.json";

        try {
            $blobRestProxy = ServicesBuilder::getInstance()->createBlobService($connectionString);
            $blob = $blobRestProxy->getBlob($containerName, $blobUrl);
            $blobStream = $blob->getContentStream();
    
            $stringContent = stream_get_contents($blobStream);
            $jsonContent = json_decode($stringContent);
    
            return $jsonContent->PublishedVersion;
    
        }
        catch (ServiceException $e) {
            // Handle exception based on error codes and messages.
            // Error codes and messages are here:
            // http://msdn.microsoft.com/library/azure/dd179439.aspx
            $code = $e->getCode();
            $error_message = $e->getMessage();
            echo $code.": ".$error_message."<br />";
        }
    }

    function get_table_of_contents( $publishedVersion) {
        $article_id = "table-of-contents";
        $connectionString = get_option('vso_dps_storage_connection_string');
        $containerName = get_option('vso_dps_storage_container_name');
        $blobUrl = sprintf("en-us/%s/documentation/articles/%s.html", $publishedVersion, $article_id);

        try {
    
            $blobRestProxy = ServicesBuilder::getInstance()->createBlobService($connectionString);
            $blob = $blobRestProxy->getBlob($containerName, $blobUrl);
            $blobStream = $blob->getContentStream();
    
            $stringContent = stream_get_contents($blobStream);
    
            return $stringContent;
    
        }
        catch(ServiceException $e){
            // Handle exception based on error codes and messages.
            // Error codes and messages are here:
            // http://msdn.microsoft.com/library/azure/dd179439.aspx
            $code = $e->getCode();
            $error_message = $e->getMessage();
            echo $code.": ".$error_message."<br />";
        }
    }

    function get_article( $publishedVersion, $article_id ) {
        $connectionString = get_option('vso_dps_storage_connection_string');
        $containerName = get_option('vso_dps_storage_container_name');
        $blobUrl = sprintf("en-us/%s/documentation/articles/%s.html", $publishedVersion, $article_id);

        try {
    
            $blobRestProxy = ServicesBuilder::getInstance()->createBlobService($connectionString);
            $blob = $blobRestProxy->getBlob($containerName, $blobUrl);
            $blobStream = $blob->getContentStream();
            $blobMetadataArray = $blob->getMetadata();

            $key = 'articleTitle';
            $array = array_values($blobMetadataArray);
            $stringContent = '<h1>' . $array[1] . '</h1>';
    
            $stringContent .= stream_get_contents($blobStream);
    
            return $stringContent;
    
        }
        catch(ServiceException $e){
            // Handle exception based on error codes and messages.
            // Error codes and messages are here:
            // http://msdn.microsoft.com/library/azure/dd179439.aspx
            $code = $e->getCode();
            $error_message = $e->getMessage();
            echo $code.": ".$error_message."<br />";
        }
    }              
	
    function get_document_list() {
        $connectionString = get_option('vso_dps_storage_connection_string');

        try {
    
            $blobRestProxy = ServicesBuilder::getInstance()->createBlobService($connectionString);
            $options = new ListBlobsOptions();
            $options->setPrefix('en-us/vscom-pilot-20151126-172651/documentation/articles');
            $result = $blobRestProxy->listBlobs("vs-com-support-articles", $options);
            $blobs = $result->getBlobs();

            $stringContent = "<ul>";

            foreach($blobs as $blob)
            {
                //echo $blob->getName().": ".$blob->getUrl()."<br />";
                $stringContent .= "<li>" . $blob->getName() . "</li>";
            }

            $stringContent .= "</ul>";
    
            return $stringContent;
    
        }
        catch(ServiceException $e){
            $code = $e->getCode();
            $error_message = $e->getMessage();
            echo $code.": ".$error_message."<br />";
        }
    }
?>