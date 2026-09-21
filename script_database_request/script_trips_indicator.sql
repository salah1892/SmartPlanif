use `transport_ai`;
SELECT 
    COUNT(ID) as total_voyages,
    SUM(CASE WHEN BUS_PR IS NOT NULL AND BUS_PR != BUS_RE THEN 1 ELSE 0 END) as nb_changements_bus,
    SUM(CASE WHEN CHAUFF_PR IS NOT NULL AND CHAUFF_PR != CHAUFF_RE THEN 1 ELSE 0 END) as nb_changements_chauffeur,
    SUM(CASE WHEN CHANGEMENT = 1 THEN 1 ELSE 0 END) as nb_voyages_impactes,
    AVG(AVANCE_RETARD) as retard_moyen_secondes
FROM transport_ai.trips 
WHERE ETAT IN (3, 5); -- Voyages terminés ou avec aléas

-- requête pour calculer les indicateurs

 SELECT
     COUNT(*) AS nombre_total_voyages,

    SUM(
        CASE 
            WHEN BUS_PR IS NOT NULL 
             AND BUS_RE IS NOT NULL 
             AND BUS_PR <> BUS_RE 
            THEN 1 
            ELSE 0 
        END
    ) AS nombre_changements_bus,

    SUM(
        CASE 
            WHEN CHAUFF_PR IS NOT NULL 
             AND CHAUFF_RE IS NOT NULL 
             AND CHAUFF_PR <> CHAUFF_RE 
            THEN 1 
            ELSE 0 
        END
    ) AS nombre_changements_chauffeur,

    SUM(
        CASE 
            WHEN REC_PR IS NOT NULL 
             AND REC_RE IS NOT NULL 
             AND REC_PR <> REC_RE 
            THEN 1 
            ELSE 0 
        END
    ) AS nombre_changements_receveur,

    SUM(
        CASE 
            WHEN (
                BUS_PR IS NOT NULL 
                AND BUS_RE IS NOT NULL 
                AND BUS_PR <> BUS_RE
            )
            OR (
                CHAUFF_PR IS NOT NULL 
                AND CHAUFF_RE IS NOT NULL 
                AND CHAUFF_PR <> CHAUFF_RE
            )
            OR (
                REC_PR IS NOT NULL 
                AND REC_RE IS NOT NULL 
                AND REC_PR <> REC_RE
            )
            THEN 1 
            ELSE 0 
        END
    ) AS nombre_voyages_avec_changement_ressource,

    SUM(
        CASE 
            WHEN CHANGEMENT = 1 
            THEN 1 
            ELSE 0 
        END
    ) AS nombre_voyages_flag_changement

FROM transport_ai.trips;
-- ******************************************************
-- Requête pour calculer les taux en pourcentage
SELECT
    COUNT(*) AS total_voyages,

    ROUND(
        100.0 * SUM(
            CASE 
                WHEN BUS_PR IS NOT NULL 
                 AND BUS_RE IS NOT NULL 
                 AND BUS_PR <> BUS_RE 
                THEN 1 
                ELSE 0 
            END
        ) / NULLIF(COUNT(*), 0),
        2
    ) AS taux_changement_bus,

    ROUND(
        100.0 * SUM(
            CASE 
                WHEN CHAUFF_PR IS NOT NULL 
                 AND CHAUFF_RE IS NOT NULL 
                 AND CHAUFF_PR <> CHAUFF_RE 
                THEN 1 
                ELSE 0 
            END
        ) / NULLIF(COUNT(*), 0),
        2
    ) AS taux_changement_chauffeur,

    ROUND(
        100.0 * SUM(
            CASE 
                WHEN REC_PR IS NOT NULL 
                 AND REC_RE IS NOT NULL 
                 AND REC_PR <> REC_RE 
                THEN 1 
                ELSE 0 
            END
        ) / NULLIF(COUNT(*), 0),
        2
    ) AS taux_changement_receveur,

    ROUND(
        100.0 * SUM(
            CASE 
                WHEN (
                    BUS_PR IS NOT NULL 
                    AND BUS_RE IS NOT NULL 
                    AND BUS_PR <> BUS_RE
                )
                OR (
                    CHAUFF_PR IS NOT NULL 
                    AND CHAUFF_RE IS NOT NULL 
                    AND CHAUFF_PR <> CHAUFF_RE
                )
                OR (
                    REC_PR IS NOT NULL 
                    AND REC_RE IS NOT NULL 
                    AND REC_PR <> REC_RE
                )
                THEN 1 
                ELSE 0 
            END
        ) / NULLIF(COUNT(*), 0),
        2
    ) AS taux_changement_global_ressources,

    ROUND(
        100.0 * SUM(
            CASE 
                WHEN CHANGEMENT = 1 
                THEN 1 
                ELSE 0 
            END
        ) / NULLIF(COUNT(*), 0),
        2
    ) AS taux_flag_changement

FROM transport_ai.trips;

-- ******************************************************
-- Taux de changement Bus & Chauffeurs
SELECT 
    COUNT(*) AS total_voyages,
    SUM(CASE WHEN BUS_PR <> BUS_RE THEN 1 ELSE 0 END) AS nb_changements_bus,
    SUM(CASE WHEN CHAUFF_PR <> CHAUFF_RE THEN 1 ELSE 0 END) AS nb_changements_chauffeur,
    SUM(CASE WHEN REC_PR <> REC_RE THEN 1 ELSE 0 END) AS nb_changements_receveur,
    ROUND((SUM(CASE WHEN BUS_PR <> BUS_RE THEN 1 ELSE 0 END) * 100.0 / COUNT(*)), 2) AS taux_changement_bus_pct,
    ROUND((SUM(CASE WHEN CHAUFF_PR <> CHAUFF_RE THEN 1 ELSE 0 END) * 100.0 / COUNT(*)), 2) AS taux_changement_chauffeur_pct,
    ROUND((SUM(CASE WHEN REC_PR <> REC_RE THEN 1 ELSE 0 END) * 100.0 / COUNT(*)), 2) AS taux_changement_receveur_pct
FROM transport_ai.trips
WHERE ETAT = 'Exécuté';
-- ******************************************************
-- Analyse de la ponctualité au départ
SELECT 
    DENUMLI,
    AVG(AVANCE_RETARD) AS retard_moyen_minutes,
    SUM(CASE WHEN AVANCE_RETARD > 5 THEN 1 ELSE 0 END) AS nb_retards_critiques,
    ROUND((SUM(CASE WHEN AVANCE_RETARD <= 5 AND AVANCE_RETARD >= -2 THEN 1 ELSE 0 END) * 100.0 / COUNT(*)), 2) AS taux_ponctualite_pct
FROM transport_ai.trips
GROUP BY DENUMLI;
-- ******************************************************
-- Taux de réalisation effectif
SELECT 
    GRP,
    COUNT(*) AS total_programmes,
    SUM(CASE WHEN ETAT = 'Exécuté' AND DEVALID = 0 THEN 1 ELSE 0 END) AS total_executes,
    SUM(CASE WHEN DEVALID = 1 OR ETAT = 'Annulé' THEN 1 ELSE 0 END) AS total_annules,
    ROUND((SUM(CASE WHEN ETAT = 'Exécuté' AND DEVALID = 0 THEN 1 ELSE 0 END) * 100.0 / COUNT(*)), 2) AS taux_realisation_pct
FROM transport_ai.trips
GROUP BY GRP;