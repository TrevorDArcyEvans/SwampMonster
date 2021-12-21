var coll = document.getElementsByClassName("collapsible");

for (var i = 0; i < coll.length; i++)
{
  coll[i].addEventListener("click", function ()
  {
    this.classList.toggle("active");
    var content = this.nextElementSibling;
    if (content.style.maxHeight)
    {
      content.style.maxHeight = null;
    }
    else
    {
      content.style.maxHeight = content.scrollHeight + "px";
    }
  });

  // expand by default
  var content = coll[i].nextElementSibling;
  content.style.maxHeight = content.scrollHeight + "px";
  coll[i].classList.toggle("active");
}
