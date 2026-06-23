#!/bin/sh
set -eu

: "${WEBAPP_NAME:=LittleHelpers}"
: "${WEBAPP_SHORT_NAME:=$WEBAPP_NAME}"
: "${WEBAPP_DESCRIPTION:=Family chore management app.}"
: "${WEBAPP_LANG:=en}"
: "${WEBAPP_TITLE:=$WEBAPP_NAME}"

export WEBAPP_NAME WEBAPP_SHORT_NAME WEBAPP_DESCRIPTION WEBAPP_LANG WEBAPP_TITLE

envsubst '${WEBAPP_NAME} ${WEBAPP_SHORT_NAME} ${WEBAPP_DESCRIPTION} ${WEBAPP_LANG}' \
    < /usr/share/nginx/html/manifest.webmanifest.template \
    > /usr/share/nginx/html/manifest.webmanifest

envsubst '${WEBAPP_TITLE} ${WEBAPP_LANG}' \
    < /usr/share/nginx/html/index.html.template \
    > /usr/share/nginx/html/index.html
