import React, { useEffect, useRef, memo } from 'react';
import './Map.scss';

const Map = memo(function Map() {
    const viewDivRef = useRef(null);
    const viewRef = useRef(null);
    const mapInitializedRef = useRef(false);

    useEffect(() => {
        if (!window.require || !viewDivRef.current || mapInitializedRef.current) {
            return;
        }

        mapInitializedRef.current = true;

        window.require([
            'esri/Map',
            'esri/views/SceneView',
            'esri/widgets/BasemapGallery',
            'esri/widgets/Expand',
            'esri/Basemap'
        ], (Map, SceneView, BasemapGallery, Expand, Basemap) => {
            if (!viewDivRef.current) return;

            const savedBasemap = localStorage.getItem('map-basemap') || 'satellite';
            const savedCamera = localStorage.getItem('map-camera');
            const defaultCamera = {
                fov: 55,
                position: {
                    latitude: 44.39006252879528,
                    longitude: 34.065645317382945,
                    z: 937.259094823151
                },
                heading: 25.521363443185887,
                tilt: 87.6187987574998
            };
            const camera = savedCamera ? JSON.parse(savedCamera) : defaultCamera;

            const map = new Map({
                basemap: savedBasemap,
                ground: 'world-elevation'
            });

            const view = new SceneView({
                container: viewDivRef.current,
                map: map,
                alphaCompositingEnabled: true,
                qualityProfile: 'high',
                camera: camera,
            });

            viewRef.current = view;

            var baseMaps = ["dark-gray", "dark-gray-3d", "dark-gray-vector", "gray", "gray-3d", "gray-vector",
                "hybrid", "navigation-3d", "navigation-dark-3d", "oceans", "osm", "osm-3d",
                "satellite", "streets", "streets-3d", "streets-dark-3d", "streets-navigation-vector",
                "streets-night-vector", "streets-relief-vector", "streets-vector", "terrain", "topo",
                "topo-3d", "topo-vector"];

            const basemaps = baseMaps.map(name => {
                const basemap = Basemap.fromId(name);
                basemap.customId = name;
                return { basemap, name };
            });

            const basemapGallery = new BasemapGallery({
                view: view,
                source: basemaps.map(item => item.basemap)
            });

            view.when(() => {
                view.map.basemap = savedBasemap;
            });

            basemapGallery.watch('activeBasemap', (newBasemap) => {
                if (newBasemap?.customId) {
                    localStorage.setItem('map-basemap', newBasemap.customId);
                }
            });

            view.watch('stationary', (isStationary) => {
                if (isStationary) {
                    const camera = view.camera;
                    localStorage.setItem('map-camera', JSON.stringify({
                        fov: camera.fov,
                        position: {
                            latitude: camera.position.latitude,
                            longitude: camera.position.longitude,
                            z: camera.position.z
                        },
                        heading: camera.heading,
                        tilt: camera.tilt
                    }));
                }
            });

            const bgExpand = new Expand({
                view: view,
                content: basemapGallery,
                expandIcon: 'basemap',
                expandTooltip: 'Basemap Gallery'
            });

            view.ui.add(bgExpand, 'top-right');
        });
    }, []);

    return (
        <div
            ref={viewDivRef}
            className="map-view-div"
        />
    );
});

export default Map;
