<?xml version="1.0" ?><xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0"><xsl:output method="text" indent="no"/><xsl:template match="/data"><![CDATA[<P>Dear Customer,</P>
					<P>As per your request, we have unsubscribed you from receiving marketing related communication from us.</P>
					<P>Please call us at ]]><xsl:choose><xsl:when test="systemuser/address1_telephone1"><xsl:value-of select="systemuser/address1_telephone1" /></xsl:when><xsl:otherwise>UserPhone</xsl:otherwise></xsl:choose><![CDATA[ for any further changes or clarifications.</P>
					<P>Thanks</P>]]></xsl:template></xsl:stylesheet>
				